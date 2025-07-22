using System.Reflection;
using System.Text;
using System.Text.Json;

namespace LlmTornado.Agents
{
    public static class ToolUtility
    {
        public static AgentTool AsTool(this Agent agent)
        {
            return new AgentTool(agent, new BaseTool().CreateTool(
                            toolName: agent.Id,
                            toolDescription: agent.Instructions,
                            toolParameters: BinaryData.FromBytes("""
                            {
                                "type": "object",
                                "properties": { "input" : {"type" : "string"}},
                                "required": [ "input" ]
                            }
                            """u8.ToArray()))
                );
        }

        public static FunctionTool ConvertFunctionToTool(this Delegate function)
        {
            MethodInfo method = function.Method;

            var toolAttrs = method.GetCustomAttributes<ToolAttribute>();

            if (toolAttrs.Count() == 0) throw new Exception("Function doesn't have Tool Attribute");

            ToolAttribute toolAttr = toolAttrs.First();

            List<string> required_inputs = new List<string>();

            int i = 0;

            var input_tool_map = new Dictionary<string, ParameterSchema>();

            foreach (ParameterInfo param in method.GetParameters())
            {
                if (param.Name == null) continue;

                string typeName = param.ParameterType.IsEnum ? "string" : json_util.MapClrTypeToJsonType(param.ParameterType);

                var schema = new ParameterSchema
                {
                    Type = typeName,
                    Description = toolAttr.In_parameters_description[i],
                    Enum = param.ParameterType.IsEnum ? param.ParameterType.GetEnumNames() : null
                };

                input_tool_map[param.Name] = schema;

                if (!param.HasDefaultValue)
                {
                    required_inputs.Add(param.Name);
                }

                i++;
            }

            string funcParamResult = JsonSchemaGenerator.BuildFunctionSchema(input_tool_map, required_inputs);

            FunctionTool newTool = new FunctionTool(
                        toolName: method.Name,
                        toolDescription: toolAttr.Description,
                        toolParameters: BinaryData.FromBytes(Encoding.UTF8.GetBytes(funcParamResult)),
                        function: function
                    );

            return newTool;
        }

        public static List<object> ParseFunctionCallArgs(this Delegate function, BinaryData functionCallArguments)
        {
            MethodInfo method = function.Method;
            List<object> arguments = new List<object>();
            using var document = JsonDocument.Parse(functionCallArguments);
            var parameters = method.GetParameters();
            var argumentsByName = document.RootElement.EnumerateObject()
                .ToDictionary(p => p.Name, p => p.Value, StringComparer.OrdinalIgnoreCase);

            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            foreach (var param in parameters)
            {
                if (!argumentsByName.TryGetValue(param.Name!, out var value))
                {
                    if (param.HasDefaultValue)
                    {
                        arguments.Add(param.DefaultValue!);
                        continue;
                    }
                    throw new JsonException($"Required parameter '{param.Name}' not found in function call arguments.");
                }

                if (param.ParameterType.IsEnum)
                {
                    var enumString = value.GetString();
                    if (Enum.TryParse(param.ParameterType, enumString, ignoreCase: true, out var enumValue))
                    {
                        arguments.Add(enumValue);
                        continue;
                    }
                    else
                    {
                        var validValues = string.Join(", ", Enum.GetNames(param.ParameterType));
                        throw new JsonException($"Invalid value '{enumString}' for enum '{param.ParameterType.Name}'. Valid values: {validValues}");
                    }
                }

                if (param.ParameterType.IsPrimitive || param.ParameterType == typeof(string) || param.ParameterType == typeof(decimal))
                {
                    arguments.Add(value.ValueKind switch
                    {
                        JsonValueKind.String => value.GetString()!,
                        JsonValueKind.Number when param.ParameterType == typeof(int) => value.GetInt32(),
                        JsonValueKind.Number when param.ParameterType == typeof(long) => value.GetInt64(),
                        JsonValueKind.Number when param.ParameterType == typeof(double) => value.GetDouble(),
                        JsonValueKind.Number when param.ParameterType == typeof(float) => value.GetSingle(),
                        JsonValueKind.True => true,
                        JsonValueKind.False => false,
                        JsonValueKind.Null when param.HasDefaultValue => param.DefaultValue!,
                        _ => throw new NotImplementedException(
                            $"Conversion from {value.ValueKind} to {param.ParameterType.Name} is not implemented.")
                    });
                }
                else
                {
                    // Try to deserialize complex types (objects, records)
                    var obj = value.Deserialize(param.ParameterType, jsonOptions);
                    arguments.Add(obj!);
                }
            }

            return arguments;
        }
    }
}
