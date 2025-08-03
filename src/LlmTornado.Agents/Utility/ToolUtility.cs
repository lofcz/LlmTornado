using LlmTornado.Code;
using LlmTornado.Common;
using System.ComponentModel;
using System.Reflection;
using System.Text.Json;
using LlmTornado.Agents.DataModels;
using LlmTornado.Infra;

namespace LlmTornado.Agents;

public static class ToolUtility
{
    public static TornadoAgentTool AsTool(this TornadoAgent agent)
    {
        return new TornadoAgentTool(agent, new Tool(new ToolFunction(
            name: agent.Id,
            description: agent.Instructions,
            parameters: """
                        {
                            "type": "object",
                            "properties": { "input" : {"type" : "string"}},
                            "additionalProperties": false,
                            "required": [ "input" ]
                        }
                        """)
        ));
    }

    public static Tool ConvertFunctionToTornadoTool(this Delegate function)
    {
        MethodInfo method = function.Method;
        List<string> required_inputs = [];
        string toolDescription = method.Name;

        if (method.IsDefined(typeof(DescriptionAttribute), false))
        {
            toolDescription = method.GetCustomAttributes<DescriptionAttribute>().First().Description;
        }

        ParameterInfo[] parameters = method.GetParameters();
        ToolMetadata toolMetadata = new ToolMetadata();
        toolMetadata.Ignore ??= [];

        foreach (ParameterInfo param in parameters)
        {
            if (param.Name == null) continue;

            if (!param.HasDefaultValue)
            {
                required_inputs.Add(param.Name);
            }

            if (param.IsDefined(typeof(SchemaIgnoreAttribute), inherit: false))
            {
                if (!toolMetadata.Ignore.Contains(param.Name))
                {
                    toolMetadata.Ignore.Add(param.Name);
                }
            }
        }

        bool strictSchema = required_inputs.Count == parameters.Length;

        return new Tool(function, method.Name, toolDescription, toolMetadata, strictSchema);
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
        List<object> arguments = [];
        using JsonDocument document = JsonDocument.Parse(functionCallArguments);
        ParameterInfo[] parameters = method.GetParameters();
        Dictionary<string, JsonElement> argumentsByName = document.RootElement.EnumerateObject()
            .ToDictionary(p => p.Name, p => p.Value, StringComparer.OrdinalIgnoreCase);

        JsonSerializerOptions jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        foreach (ParameterInfo param in parameters)
        {
            if (!argumentsByName.TryGetValue(param.Name!, out JsonElement value))
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
                string? enumString = value.GetString();

                if (EnumHelpers.TryParse(param.ParameterType, enumString, ignoreCase: true, out object? enumValue))
                {
                    arguments.Add(enumValue);
                    continue;
                }

                string validValues = string.Join(", ", Enum.GetNames(param.ParameterType));
                throw new JsonException($"Invalid value '{enumString}' for enum '{param.ParameterType.Name}'. Valid values: {validValues}");
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
                    _ => throw new NotImplementedException($"Conversion from {value.ValueKind} to {param.ParameterType.Name} is not implemented.")
                });
            }
            else
            {
                // Try to deserialize complex types (objects, records)
                object? obj = value.Deserialize(param.ParameterType, jsonOptions);
                arguments.Add(obj!);
            }
        }

        return arguments;
    }
}