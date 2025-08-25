using LlmTornado.Agents.DataModels;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Common;
using System.ComponentModel;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace LlmTornado.Agents;

public static class JsonUtility
{
    /// <summary>
    /// Determines whether the specified string is a valid JSON format.
    /// </summary>
    /// <remarks>This method attempts to parse the input string as JSON. If parsing succeeds without
    /// exceptions, the string is considered valid JSON.</remarks>
    /// <param name="jsonString">The string to validate as JSON. Cannot be null or whitespace.</param>
    /// <returns><see langword="true"/> if the specified string is valid JSON; otherwise, <see langword="false"/>.</returns>
    public static bool IsValidJson(string jsonString)
    {
        if (string.IsNullOrWhiteSpace(jsonString))
        {
            return false;
        }
        try
        {
            // Attempt to parse the JSON string
            JsonDocument.Parse(jsonString);
            return true;
        }
        catch (JsonException)
        {
            // If a JsonException is caught, the string is not valid JSON
            return false;
        }
    }


    /// <summary>
    /// Creates a <see cref="ChatRequestResponseFormats"/> instance representing the JSON schema of the specified type.
    /// </summary>
    /// <remarks>If the specified type has a <see cref="DescriptionAttribute"/>, the first description
    /// found is included in the format description.</remarks>
    /// <param name="type">The type for which to generate the JSON schema.</param>
    /// <param name="jsonSchemaIsStrict">A boolean value indicating whether the generated JSON schema should be strict.  <see langword="true"/> if
    /// the schema should enforce strict validation; otherwise, <see langword="false"/>.</param>
    /// <returns>A <see cref="ChatRequestResponseFormats"/> containing the JSON schema of the specified type, encoded as binary data. Used for output formating</returns>
    public static ChatRequestResponseFormats CreateJsonSchemaFormatFromType(this Type type, bool jsonSchemaIsStrict = true)
    {
        string formatDescription = "";
        IEnumerable<DescriptionAttribute> descriptions = type.GetCustomAttributes<DescriptionAttribute>();
        if (descriptions.Any())
        {
            formatDescription = descriptions.First().Description;
        }

        dynamic? responseFormat = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(JsonSchemaGenerator.GenerateSchema(type));

        return ChatRequestResponseFormats.StructuredJson(
            type.Name,
            responseFormat,
            formatDescription,
            jsonSchemaIsStrict
        );
    }
    
    /// <summary>
    /// Deserializes the JSON string into an object of the specified type.
    /// </summary>
    /// <typeparam name="T">The type of the object to deserialize to.</typeparam>
    /// <param name="json">The JSON string to deserialize. Cannot be null or empty.</param>
    /// <returns>An object of type <typeparamref name="T"/> deserialized from the JSON string.</returns>
    /// <exception cref="ArgumentException">Thrown if <paramref name="json"/> is null or empty.</exception>
    public static T ParseJson<T>(this string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            throw new ArgumentException("JSON is null or empty");

        if( !IsValidJson(json))
            throw new JsonException("Invalid JSON format");

        if(json.TryParseJson<T>( out T? result))
        {
            return result!;
        }

        return JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            AllowTrailingCommas = true,
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Skip
        })!;
    }
    
    /// <summary>
    /// Attempts to parse the specified JSON string into an object of type <typeparamref name="T"/>.
    /// </summary>
    /// <remarks>This method does not throw exceptions for invalid JSON strings. Instead, it returns
    /// <see langword="false"/> and sets  <paramref name="result"/> to the default value of <typeparamref
    /// name="T"/>.</remarks>
    /// <typeparam name="T">The type of the object to deserialize the JSON string into.</typeparam>
    /// <param name="json">The JSON string to parse. Cannot be null or whitespace.</param>
    /// <param name="result">When this method returns, contains the object of type <typeparamref name="T"/> that is deserialized from the
    /// JSON string, if the parsing is successful; otherwise, the default value for type <typeparamref name="T"/>.</param>
    /// <returns><see langword="true"/> if the JSON string was successfully parsed into an object of type <typeparamref
    /// name="T"/>;  otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentException">Thrown if <paramref name="json"/> is null or whitespace.</exception>
    public static bool TryParseJson<T>(this string? json, out T? result)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(json))
                throw new ArgumentException("JSON is null or empty");
            result = JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true, AllowTrailingCommas = true, UnmappedMemberHandling = JsonUnmappedMemberHandling.Skip });// 👈 This is the key});
            return true;
        }
        catch (Exception)
        {
            result = default;
            return false;
        }
    }

    /// <summary>
    /// Maps a CLR type to its corresponding JSON type representation.
    /// </summary>
    /// <param name="type">The CLR <see cref="Type"/> to map to a JSON type.</param>
    /// <returns>A <see cref="string"/> representing the JSON type: "string" for <see cref="string"/>,  "boolean" for <see
    /// cref="bool"/>, "number" for numeric types, "array" for array types,  and "object" for other complex types.</returns>
    public static string MapClrTypeToJsonType(Type type)
    {
        if (type == typeof(string)) return "string";
        if (type == typeof(bool)) return "boolean";
        if (type.IsPrimitive || type == typeof(decimal) || type == typeof(double) || type == typeof(float)) return "number";
        if (type.IsArray) return "array";
        return "object"; // fallback for complex types
    }

    /// <summary>
    /// Attempts to parse a JSON string into an object of type <typeparamref name="T"/>.  If the input is invalid JSON,
    /// the method attempts to repair it using an external agent.
    /// </summary>
    /// <remarks>This method first attempts to parse the input JSON string directly. If the input is invalid
    /// JSON,  it uses the provided <see cref="TornadoAgent"/> to attempt to repair the JSON and then retries parsing.
    /// The method performs basic cleanup on the input string, such as removing Markdown code fences and trimming
    /// whitespace.</remarks>
    /// <typeparam name="T">The type to which the JSON string should be deserialized.</typeparam>
    /// <param name="agent">An instance of <see cref="TornadoAgent"/> used to repair invalid JSON if the initial parsing fails. (Can be any agent will restore after use)</param>
    /// <param name="possibleJson">A string that is expected to contain JSON data. The string may include extraneous formatting, such as Markdown
    /// code fences.</param>
    /// <returns>An object of type <typeparamref name="T"/> if the JSON is successfully parsed or repaired;  otherwise, <see
    /// langword="null"/> if parsing and repair attempts fail.</returns>
    public static async Task<T?> SmartParseJsonAsync<T>(TornadoAgent agent, string possibleJson)
    {
        if (string.IsNullOrWhiteSpace(possibleJson))
            throw new ArgumentException("JSON is null or empty");

        if (possibleJson.TryParseJson<T>(out T? result))
        {
            return result!;
        }

        string lastInstructions = agent.Instructions;
        Type? type = agent.OutputSchema;
        List<Tool> tools = agent.Options.Tools?.ToList() ?? [];
        agent.OutputSchema = null; // Clear output schema for this operation to avoid conflicts
        agent.Options.Tools = new List<Tool>(); // Clear tools for this operation to avoid conflicts
        try
        {
            
            // Basic cleanup - remove Markdown code fences and leading/trailing whitespace
            string cleaned = possibleJson.Trim();
            cleaned = Regex.Replace(cleaned, @"^```json\s*|```$", "", RegexOptions.Multiline);

            // Check if it's valid JSON already
            try
            {
                JsonDocument.Parse(cleaned);
                return JsonSerializer.Deserialize<T>(cleaned, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    AllowTrailingCommas = true
                }) ?? throw new JsonException("Deserialized result is null");
            }
            catch (JsonException) { /* Continue with repair attempts */ }

            // If basic cleaning didn't work, we can use the LLM itself to repair the JSON
            string repairPrompt = $"Fix this invalid JSON to match the C# type {typeof(T).Name}. " +
                                  $"Return ONLY the fixed JSON with no explanations or markdown:\n{cleaned}";

            agent.Instructions = "You are a JSON repair agent. Your task is to fix invalid JSON strings to match the C# type provided. " +
                                  "Return ONLY the fixed JSON with no explanations or markdown.";

            Conversation repairResult = await TornadoRunner.RunAsync(agent, repairPrompt);
            agent.Instructions = lastInstructions; // Restore original instructions
            agent.OutputSchema = type; // Restore original output schema
            agent.Options.Tools = tools; // Restore original tools
            // Clean the repair result
            string repairedJson = repairResult.Messages.Last().Content?.Trim() ?? "";
            repairedJson = Regex.Replace(repairedJson, @"^```json\s*|```$", "", RegexOptions.Multiline);

            // Validate the repaired JSON
            try
            {
                JsonDocument.Parse(repairedJson);
                return JsonSerializer.Deserialize<T>(cleaned, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    AllowTrailingCommas = true,
                    UnmappedMemberHandling = JsonUnmappedMemberHandling.Skip
                }) ?? default!;
            }
            catch (JsonException)
            {
                return default!;
            }
        }
        catch (Exception ex)
        {
            agent.Instructions = lastInstructions; // Restore original instructions
            agent.OutputSchema = type; // Restore original output schema
            agent.Options.Tools = tools; // Restore original tools
            return default!;
        }
    }

    /// <summary>
    /// Use an agent to attempt to parse or repair a JSON string into an object of type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="possibleJson"></param>
    /// <param name="agent"></param>
    /// <returns></returns>
    public static async Task<T?> SmartParseJsonAsync<T>(this string possibleJson, TornadoAgent agent)
    {
        return await SmartParseJsonAsync<T>(agent, possibleJson);
    }


}

/// <summary>
/// Represents the schema for a parameter, including its type, description, and possible enumeration values.
/// </summary>
/// <remarks>This class is used to define the structure of a parameter, specifying its data type, a
/// textual description, and an optional set of allowed values. The <see cref="Enum"/> property is ignored during
/// JSON serialization if it is null.</remarks>
public class ParameterSchema
{
    [JsonPropertyName("type")]
    public string Type { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; }

    [JsonPropertyName("enum")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string[]? Enum { get; set; }
}

public static class JsonSchemaGenerator
{
    /// <summary>
    /// Constructs a JSON schema for a function based on specified properties and required fields.
    /// </summary>
    /// <param name="properties">A dictionary containing the properties of the schema, where the key is the property name and the value is
    /// the <see cref="ParameterSchema"/> defining the property's characteristics.</param>
    /// <param name="required">A list of property names that are required in the schema.</param>
    /// <returns>A JSON string representing the schema, formatted with indentation for readability.</returns>
    internal static string BuildFunctionSchema(Dictionary<string, ParameterSchema> properties, List<string> required)
    {
        var schema = new
        {
            type = "object",
            properties = properties.ToDictionary(
                kvp => kvp.Key, object (kvp) => kvp.Value
            ),
            required = required,
            additionalProperties = false
        };

        return JsonSerializer.Serialize(schema, new JsonSerializerOptions { WriteIndented = true });
    }

    /// <summary>
    /// Generates a JSON schema for the specified type.
    /// </summary>
    /// <remarks>The generated schema includes the type as an object, its properties, and required
    /// fields.  Additional properties are not allowed in the schema.</remarks>
    /// <param name="type">The type for which to generate the JSON schema. Must not be <see langword="null"/>.</param>
    /// <returns>A JSON string representing the schema of the specified type, formatted with indentation for readability.</returns>
    public static string GenerateSchema(Type type)
    {
        Dictionary<string, object> schema = new Dictionary<string, object>
        {
            ["type"] = "object",
            ["properties"] = GetPropertiesSchema(type),
            ["required"] = GetRequiredProperties(type),
            ["additionalProperties"] = false
        };

        return JsonSerializer.Serialize(schema, new JsonSerializerOptions
        {
            WriteIndented = true
        });
    }

    /// <summary>
    /// Generates a schema representation of the properties of a specified type.
    /// </summary>
    /// <param name="type">The type whose properties are to be represented in the schema.</param>
    /// <param name="fromArray">A boolean value indicating whether the schema should be generated as if the type is part of an array. If
    /// <see langword="true"/>, additional schema elements such as "type", "properties", "required", and
    /// "additionalProperties" are included.</param>
    /// <returns>A dictionary representing the schema of the properties of the specified type. If <paramref
    /// name="fromArray"/> is <see langword="true"/>, the dictionary includes additional schema elements.</returns>
    private static Dictionary<string, object> GetPropertiesSchema(Type type, bool fromArray = false)
    {
        Dictionary<string, object> properties = new Dictionary<string, object>();
        if (fromArray)
        {
            string jsonType = JsonUtility.MapClrTypeToJsonType(type);
            properties.Add("type", jsonType);

            if (jsonType == "object")
            {
                Dictionary<string, object> subProperties = new Dictionary<string, object>();
                foreach (PropertyInfo prop in type.GetProperties())
                {
                    if (prop.PropertyType.Name == type.Name)
                    {
                        throw new ArgumentException($"Infinite Recursion detected please fix nesting on {prop.Name} in Type {type.Name}.");
                    }
                    subProperties[prop.Name] = GetPropertySchema(prop);
                }
                properties.Add("properties", subProperties);
                properties.Add("required", GetRequiredProperties(type));
                properties.Add("additionalProperties", false);
            }

            return properties;
        }
        foreach (PropertyInfo prop in type.GetProperties())
        {
            properties[prop.Name] = GetPropertySchema(prop);
        }
        return properties;
    }

    /// <summary>
    /// Generates a schema representation for a given property.
    /// </summary>
    /// <remarks>The method analyzes the property's type and attributes to construct a schema. It
    /// supports basic types such as string, boolean, and numeric types, as well as arrays and complex objects. For
    /// complex objects, it recursively generates schemas for nested properties.</remarks>
    /// <param name="prop">The <see cref="PropertyInfo"/> object representing the property for which the schema is generated.</param>
    /// <returns>A dictionary containing the schema details of the property, including type, description, and nested
    /// properties if applicable.</returns>
    private static object GetPropertySchema(PropertyInfo prop)
    {
        Dictionary<string, object> props = new Dictionary<string, object>();
        IEnumerable<DescriptionAttribute> descriptions = prop.GetCustomAttributes<DescriptionAttribute>();
        if (descriptions.Any())
        {
            props.Add("description", descriptions.First().Description);
        }
        if (prop.PropertyType == typeof(string)) props.Add("type", "string");
        else if (prop.PropertyType == typeof(bool)) props.Add("type", "boolean");
        else if (prop.PropertyType.IsNumeric()) props.Add("type", "number");
        else if (prop.PropertyType == typeof(DateTime)) props.Add("type", "string"); // DateTime is often represented as a string in JSON
        else if (prop.PropertyType == typeof(Guid)) props.Add("type", "string"); // Guid is also often represented as a string in JSON
        else if (prop.PropertyType.IsEnum)
        {
            props.Add("type", "string");
            props.Add("enum", Enum.GetNames(prop.PropertyType));
        }
        else if (prop.PropertyType.IsArray)
        {
            props.Add("type", "array");
            Type? itemType = prop.PropertyType.GetElementType();
            props.Add("items", GetPropertiesSchema(itemType, true));
        }
        else
        {
            // Fallback for nested objects
            props = new Dictionary<string, object>
            {
                ["type"] = "object",
                ["properties"] = GetPropertiesSchema(prop.PropertyType),
                ["required"] = GetRequiredProperties(prop.PropertyType),
                ["additionalProperties"] = false
            };
        }

        return props;
    }

    /// <summary>
    /// Retrieves the names of all properties defined on the specified type.
    /// </summary>
    /// <param name="type">The type whose property names are to be retrieved. Cannot be <see langword="null"/>.</param>
    /// <returns>A list of strings containing the names of all properties defined on the specified type.</returns>
    private static List<string> GetRequiredProperties(Type type)
    {
        return type.GetProperties().Select(p => p.Name).ToList();
    }

    /// <summary>
    /// Represents a collection of numeric types recognized by the system.
    /// </summary>
    /// <remarks>This set includes common numeric types such as <see cref="int"/>, <see
    /// cref="double"/>,  <see cref="decimal"/>, and others. It is used to determine if a given type is considered
    /// numeric.</remarks>
    private static readonly HashSet<Type> NumericTypes =
    [
        typeof(int), typeof(double), typeof(decimal),
        typeof(long), typeof(short), typeof(sbyte),
        typeof(byte), typeof(ulong), typeof(ushort),
        typeof(uint), typeof(float), typeof(ushort), typeof(uint),
        typeof(ulong), typeof(float)
    ];

    /// <summary>
    /// Determines whether the specified <see cref="Type"/> represents a numeric data type.
    /// </summary>
    /// <remarks>This method checks if the provided type, including nullable types, is considered
    /// numeric. Numeric types typically include integral and floating-point types.</remarks>
    /// <param name="myType">The <see cref="Type"/> to evaluate.</param>
    /// <returns><see langword="true"/> if <paramref name="myType"/> is a numeric type; otherwise, <see langword="false"/>.</returns>
    public static bool IsNumeric(this Type myType)
    {
        return NumericTypes.Contains(Nullable.GetUnderlyingType(myType) ?? myType);
    }
}