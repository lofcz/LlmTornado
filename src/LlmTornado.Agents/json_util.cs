using LlmTornado.Moderation;
using System.ComponentModel;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LlmTornado.Agents
{
    public static class json_util
    {
        /// <summary>
        /// Creates a <see cref="ModelOutputFormat"/> instance representing the JSON schema of the specified type.
        /// </summary>
        /// <remarks>If the specified type has a <see cref="DescriptionAttribute"/>, the first description
        /// found is included in the format description.</remarks>
        /// <param name="type">The type for which to generate the JSON schema.</param>
        /// <param name="jsonSchemaIsStrict">A boolean value indicating whether the generated JSON schema should be strict.  <see langword="true"/> if
        /// the schema should enforce strict validation; otherwise, <see langword="false"/>.</param>
        /// <returns>A <see cref="ModelOutputFormat"/> containing the JSON schema of the specified type, encoded as binary data. Used for output formating</returns>
        public static ModelOutputFormat CreateJsonSchemaFormatFromType(this Type type, bool jsonSchemaIsStrict = true)
        {
            string formatDescription = "";
            var descriptions = type.GetCustomAttributes<DescriptionAttribute>();
            if(descriptions.Count() > 0)
            {
                formatDescription = descriptions.First().Description;
            }
            return new ModelOutputFormat(
                type.Name,
                BinaryData.FromBytes(Encoding.UTF8.GetBytes(JsonSchemaGenerator.GenerateSchema(type))),
                jsonSchemaIsStrict,
                formatDescription
                );
        }

        /// <summary>
        /// Deserializes the JSON text from the <see cref="RunResult"/> into an object of type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type of the object to deserialize the JSON text into.</typeparam>
        /// <param name="Result">The <see cref="RunResult"/> containing the JSON text to deserialize.</param>
        /// <returns>An object of type <typeparamref name="T"/> deserialized from the JSON text.</returns>
        /// <exception cref="ArgumentException">Thrown if the <see cref="RunResult.Text"/> is null or empty.</exception>
        public static T ParseJson<T>(this RunResult Result)
        {
            string json = Result.Text;

            if (string.IsNullOrWhiteSpace(json))
                throw new ArgumentException("RunResult Text is null or empty");

            return JsonSerializer.Deserialize<T>(json)!;
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

            return JsonSerializer.Deserialize<T>(json)!;
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
                result = JsonSerializer.Deserialize<T>(json);
                return true;
            }
            catch (Exception)
            {
                result = default;
                return false;
            }
        }


        /// <summary>
        /// Attempts to parse the JSON text from the specified <see cref="RunResult"/> into an object of type
        /// <typeparamref name="T"/>.
        /// </summary>
        /// <remarks>This method catches any exceptions that occur during deserialization and returns <see
        /// langword="false"/> if an error occurs.</remarks>
        /// <typeparam name="T">The type of the object to deserialize the JSON text into.</typeparam>
        /// <param name="Result">The <see cref="RunResult"/> containing the JSON text to parse.</param>
        /// <param name="result">When this method returns, contains the deserialized object of type <typeparamref name="T"/> if the parsing
        /// is successful; otherwise, the default value for type <typeparamref name="T"/>.</param>
        /// <returns><see langword="true"/> if the JSON text is successfully parsed; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentException">Thrown if the JSON text in <paramref name="Result"/> is null or empty.</exception>
        public static bool TryParseJson<T>(this RunResult Result, out T? result)
        {
            try
            {
                string json = Result.Text;
                if (string.IsNullOrWhiteSpace(json))
                    throw new ArgumentException("JSON is null or empty");
                result = JsonSerializer.Deserialize<T>(json);
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
                    kvp => kvp.Key,
                    kvp => (object)kvp.Value
                ),
                required,
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
            var schema = new Dictionary<string, object>
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
            var properties = new Dictionary<string, object>();
            if (fromArray)
            {
                properties.Add("type", json_util.MapClrTypeToJsonType(type));
                var subProperties = new Dictionary<string, object>();
                foreach (var prop in type.GetProperties())
                {
                    subProperties[prop.Name] = GetPropertySchema(prop);
                }
                properties.Add("properties", subProperties);
                properties.Add("required", GetRequiredProperties(type));
                properties.Add("additionalProperties", false);
                return properties;
            }
            foreach (var prop in type.GetProperties())
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
            var props = new Dictionary<string, object>();
            //var descriptions = prop.GetCustomAttributes<DescriptionAttribute>();
            //if(descriptions.Count() > 0)
            //{
            //    props.Add("description", descriptions.First().Description);
            //}
            if (prop.PropertyType == typeof(string)) props.Add("type", "string");
            else if (prop.PropertyType == typeof(bool)) props.Add("type", "boolean");
            else if (prop.PropertyType.IsNumeric()) props.Add("type", "number");
            else if(prop.PropertyType == typeof(DateTime)) props.Add("type", "string"); // DateTime is often represented as a string in JSON
            else if (prop.PropertyType == typeof(Guid)) props.Add("type", "string"); // Guid is also often represented as a string in JSON
            else if (prop.PropertyType.IsEnum)
            {
                props.Add("type", "string");
                props.Add("enum", Enum.GetNames(prop.PropertyType));
            }
            else if (prop.PropertyType.IsArray)
            {
                props.Add("type", "array");
                var itemType = prop.PropertyType.GetElementType();
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
        private static readonly HashSet<Type> NumericTypes = new HashSet<Type>
        {
            typeof(int),  typeof(double),  typeof(decimal),
            typeof(long), typeof(short),   typeof(sbyte),
            typeof(byte), typeof(ulong),   typeof(ushort),
            typeof(uint), typeof(float), typeof(ushort), typeof(uint),
            typeof(ulong), typeof(float)
        };

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
}
