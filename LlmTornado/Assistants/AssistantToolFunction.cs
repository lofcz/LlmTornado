using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LlmTornado.Assistants;

/// <summary>
///     Represents a tool object of type function
/// </summary>
public class AssistantToolFunction : AssistantTool
{
    /// <summary>
    ///     Creates a new function type tool.
    /// </summary>
    /// <param name="functionConfig"></param>
    public AssistantToolFunction(ToolFunctionConfig functionConfig)
    {
        Type = "function";
        FunctionConfig = functionConfig;
    }

    /// <summary>
    ///     Creates a new function type tool with preconfigured parameters
    /// </summary>
    /// <param name="name"></param>
    /// <param name="description"></param>
    /// <param name="parameters"></param>
    /// <param name="strict"></param>
    public AssistantToolFunction(string name, string description, object parameters, bool strict = false) : this(
        new ToolFunctionConfig(name, description, parameters, strict))
    {
    }

    /// <summary>
    ///     Configuration for the function tool
    /// </summary>
    [JsonProperty("function", Required = Required.Default)]
    public ToolFunctionConfig? FunctionConfig { get; set; }
}

/// <summary>
///     Represents a Tool function object for the OpenAI API.
///     A tool contains information about the function to be called, its description and parameters.
/// </summary>
/// <remarks>
///     The 'Name' property represents the name of the function and must consist of alphanumeric characters, underscores,
///     or dashes, with a maximum length of 64.
///     The 'Description' property is an optional field that provides a brief explanation about what the function does.
///     The 'Parameters' property describes the parameters that the function accepts, which are represented as a JSON
///     Schema object.
///     Various types of input are acceptable for the 'Parameters' property, such as a JObject, a Dictionary of string and
///     object, an anonymous object, or any other serializable object.
///     If the object is not a JObject, it will be converted into a JObject.
///     Refer to the 'Parameters' property setter for more details.
///     Refer to the OpenAI API <see href="https://platform.openai.com/docs/guides/gpt/function-calling">guide</see> and
///     the
///     JSON Schema <see href="https://json-schema.org/understanding-json-schema/">reference</see> for more details on the
///     format of the parameters.
/// </remarks>
public class ToolFunctionConfig
{
    private static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings
        {NullValueHandling = NullValueHandling.Ignore};

    /// <summary>
    ///     The name of the function to be called. Must be a-z, A-Z, 0-9, or contain underscores and dashes, with a maximum
    ///     length of 64.
    /// </summary>
    [JsonProperty("name", Required = Required.Always)]
    public string Name { get; set; }

    /// <summary>
    ///     The description of what the function does.
    /// </summary>
    [JsonProperty("description")]
    public string Description { get; set; }

    /// <summary>
    ///     The input parameters of the tool, if any.
    /// </summary>
    [JsonProperty("parameters")]
    public JObject? Parameters { get; set; }

    /// <summary>
    ///     Whether to enable strict schema adherence when generating the function call.
    ///     If set to true, the model will follow the exact schema defined in the parameters
    /// </summary>
    [JsonProperty("strict")]
    public bool? Strict { get; set; }

    [JsonIgnore] internal object? RawParameters { get; set; }

    /// <summary>
    ///     Create a parameterless function.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="description"></param>
    /// <param name="strict"></param>
    public ToolFunctionConfig(string name, string description, bool strict = false)
    {
        Name = name;
        Description = description;
        Parameters = null;
        Strict = strict;
    }

    /// <summary>
    ///     Create a function with parameters.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="description"></param>
    /// <param name="parameters">JSON serialized object, will be deserialized into <see cref="JObject" /> </param>
    /// <param name="strict"></param>
    public ToolFunctionConfig(string name, string description, string parameters, bool strict = false)
    {
        Name = name;
        Description = description;
        Parameters = JObject.Parse(parameters);
        Strict = strict;
    }

    /// <summary>
    ///     Create a function with parameters.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="description"></param>
    /// <param name="parameters"></param>
    public ToolFunctionConfig(string name, string description, JObject parameters)
    {
        Name = name;
        Description = description;
        Parameters = parameters;
        RawParameters = parameters;
    }

    /// <summary>
    ///     Create a function with parameters.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="description"></param>
    /// <param name="parameters">A JSON-serializable object</param>
    /// <param name="strict"></param>
    public ToolFunctionConfig(string name, string description, object parameters, bool strict = false)
    {
        Name = name;
        Description = description;
        Parameters = JObject.FromObject(parameters, JsonSerializer.Create(SerializerSettings));
        Strict = strict;
    }

    /// <summary>
    ///     Creates an empty Function object.
    /// </summary>
    private ToolFunctionConfig()
    {
    }
}