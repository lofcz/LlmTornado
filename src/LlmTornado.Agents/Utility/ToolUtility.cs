using NUnit.Framework.Interfaces;
using System.ComponentModel;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace LlmTornado.Agents
{
    public static class ToolUtility
    {
        public static TornadoAgentTool AsTool(this TornadoAgent agent)
        {
            return new TornadoAgentTool(agent, new BaseTool().CreateTool(
                            toolName: agent.Id,
                            toolDescription: agent.Instructions,
                            toolParameters: BinaryData.FromBytes("""
                            {
                                "type": "object",
                                "properties": { "input" : {"type" : "string"}},
                                "additionalProperties": false,
                                "required": [ "input" ]
                            }
                            """u8.ToArray()))
                );
        }
        /// <summary>
        /// Converts a delegate function into a <see cref="FunctionTool"/> object.
        /// </summary>
        /// <remarks>The method extracts metadata from the function's parameters and attributes to
        /// construct a <see cref="FunctionTool"/>.  It maps parameter types to JSON schema types and includes
        /// descriptions from the <see cref="ToolAttribute"/>.</remarks>
        /// <param name="function">The delegate function to convert. Must have a <see cref="ToolAttribute"/> applied.</param>
        /// <returns>A <see cref="FunctionTool"/> representing the specified function, including its name, description, and
        /// parameter schema.</returns>
        /// <exception cref="Exception">Thrown if the function does not have a <see cref="ToolAttribute"/>.</exception>
        public static FunctionTool ConvertFunctionToTool(this Delegate function)
        {
            MethodInfo method = function.Method;

            string toolDescription = method.Name;

            if (method.IsDefined(typeof(DescriptionAttribute), false)) 
            {
                toolDescription = method.GetCustomAttributes<DescriptionAttribute>().First().Description;
            }

            List<string> required_inputs = new List<string>();


            var input_tool_map = new Dictionary<string, ParameterSchema>();

            foreach (ParameterInfo param in method.GetParameters())
            {
                if (param.Name == null) continue;

                string typeName = param.ParameterType.IsEnum ? "string" : JsonUtility.MapClrTypeToJsonType(param.ParameterType);

                string paramDescription = param.Name;
                if (param.IsDefined(typeof(DescriptionAttribute), inherit: false))
                {
                    paramDescription = param.GetCustomAttributes<DescriptionAttribute>().First().Description;
                }

                var schema = new ParameterSchema
                {
                    Type = typeName,
                    Description = paramDescription,
                    Enum = param.ParameterType.IsEnum ? param.ParameterType.GetEnumNames() : null
                };

                input_tool_map[param.Name] = schema;
                required_inputs.Add(param.Name);
                //if (!param.HasDefaultValue)
                //{
                //    required_inputs.Add(param.Name);
                //}
            }

            string funcParamResult = JsonSchemaGenerator.BuildFunctionSchema(input_tool_map, required_inputs);

            var strictSchema = required_inputs.Count == input_tool_map.Count;

            FunctionTool newTool = new FunctionTool(
                        toolName: method.Name,
                        toolDescription: toolDescription,
                        toolParameters: BinaryData.FromBytes(Encoding.UTF8.GetBytes(funcParamResult)),
                        function: function,
                        strictSchema: true
                    );

            return newTool;
        }

        /// <summary>
        /// Parses the function call arguments from a JSON representation and maps them to the parameters of the
        /// specified delegate.
        /// </summary>
        /// <remarks>This method attempts to match JSON properties to the delegate's method parameters by
        /// name, ignoring case. It supports primitive types, strings, decimals, and enums, as well as complex types
        /// that can be deserialized from JSON. If a required parameter is missing and does not have a default value, a
        /// <see cref="JsonException"/> is thrown.</remarks>
        /// <param name="function">The delegate whose method parameters are to be matched with the provided arguments.</param>
        /// <param name="functionCallArguments">A <see cref="BinaryData"/> object containing the JSON representation of the function call arguments.</param>
        /// <returns>A list of objects representing the parsed arguments, ready to be passed to the delegate's method.</returns>
        /// <exception cref="JsonException">Thrown if a required parameter is not found in the JSON arguments, or if an invalid value is provided for an
        /// enum parameter.</exception>
        /// <exception cref="NotImplementedException"></exception>
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
