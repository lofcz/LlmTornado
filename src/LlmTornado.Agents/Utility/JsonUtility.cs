using LlmTornado.Agents.DataModels;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Common;
using LlmTornado.Infra;
using System.ComponentModel;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using LlmTornado.Code;
using LlmTornado.Code.Vendor;

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
            JsonDocument.Parse(jsonString, new JsonDocumentOptions { AllowTrailingCommas= true});
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
    public static ChatRequestResponseFormats CreateJsonSchemaFormatFromType(this Type type, bool jsonSchemaIsStrict)
    {
        string formatDescription = "";
        IEnumerable<DescriptionAttribute> descriptions = type.GetCustomAttributes<DescriptionAttribute>();
        if (descriptions.Any())
        {
            formatDescription = descriptions.First().Description;
        }

        // Use ToolFactory infrastructure to generate schema params, just like in ChatRequestResponseFormats.StructuredJson demos
        var toolParams = CreateToolParamsFromType(type);

        return ChatRequestResponseFormats.StructuredJson(toolParams, type.Name, formatDescription, jsonSchemaIsStrict);
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
    public static ChatRequestResponseFormats CreateJsonSchemaFormatFromType(this Type type)
    {
        string formatDescription = "";

        IEnumerable<DescriptionAttribute> descriptions = type.GetCustomAttributes<DescriptionAttribute>();
        if (descriptions.Any())
        {
            formatDescription = descriptions.First().Description;
        }

        // Use ToolFactory infrastructure to generate schema params, just like in ChatRequestResponseFormats.StructuredJson demos
        var toolParams = CreateToolParamsFromType(type);

        // Default to strict schema
        return ChatRequestResponseFormats.StructuredJson(toolParams, type.Name, formatDescription, strict: true);
    }
    
    /// <summary>
    /// Creates a list of ToolParam from a Type using ToolFactory infrastructure.
    /// This is the unified way to generate schemas for both tools and structured outputs.
    /// </summary>
    private static List<ToolParam> CreateToolParamsFromType(Type type)
    {
        var toolParams = new List<ToolParam>();
        var provider = new OpenAiEndpointProvider(LLmProviders.Unknown);
        
        PropertyInfo[] props = type.GetProperties();
        
        foreach (PropertyInfo property in props)
        {
            string? description = property.GetCustomAttributes<DescriptionAttribute>().FirstOrDefault()?.Description;
            
            IToolParamType paramType = ToolFactory.GetParamFromType(
                del: null,
                type: property.PropertyType,
                description: description,
                par: null,
                prop: property,
                recursionLevel: 0,
                topLevelType: property.PropertyType,
                provider: provider
            );
            
            // Set required based on nullability
#if MODERN
            var nullabilityContext = new System.Reflection.NullabilityInfoContext();
            var nullabilityInfo = nullabilityContext.Create(property);
            paramType.Required = nullabilityInfo.WriteState is System.Reflection.NullabilityState.NotNull;
#else
            (Type? baseType, bool isNullableValueType) = ToolFactory.GetNullableBaseType(property.PropertyType);
            paramType.Required = baseType.IsValueType && !isNullableValueType;
#endif
            
            SchemaNameAttribute? schemaName = property.GetCustomAttribute<SchemaNameAttribute>();
            toolParams.Add(new ToolParam(schemaName?.Name ?? property.Name, paramType));
        }
        
        return toolParams;
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
        string workingJson = json!;
        if (string.IsNullOrWhiteSpace(workingJson))
            throw new ArgumentException("JSON is null or empty");

        //Check if valid JSON first
        if (!IsValidJson(workingJson))
        {
            //Check if there is a duplicate JSON object in the string and try to repair it
            workingJson = CheckAndRepairIfAIGeneratedDuplicateJson(workingJson);
            if (!IsValidJson(workingJson))
            {
                try
                {
                    //Try it anyways
                    return JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        AllowTrailingCommas = true,
                        UnmappedMemberHandling = JsonUnmappedMemberHandling.Skip,
                        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                    })!;
                }
                catch (JsonException)
                {
                    throw;
                }
            }
        }
            
        if(workingJson.TryParseJson<T>(out T? result))
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

    public static string CheckAndRepairIfAIGeneratedDuplicateJson(string json)
    {
        // Regular expression to match JSON objects
        string pattern = @"\{(?:[^{}]|(?<open>\{)|(?<-open>\}))*\}(?(open)(?!))";
        MatchCollection matches = Regex.Matches(json, pattern);
        if (matches.Count > 1)
        {
            // If multiple JSON objects are found, return the last one
            return matches[matches.Count - 1].Value;
        }
        // If only one or no JSON object is found, return the original string
        return json;
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
        agent.UpdateOutputSchema(null); // Clear output schema for this operation to avoid conflicts
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
            agent.UpdateOutputSchema(type); // Restore original output schema
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
            agent.UpdateOutputSchema(type); // Restore original output schema
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