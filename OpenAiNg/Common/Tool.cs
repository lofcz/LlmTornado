using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace OpenAiNg.Common;

/// <summary>
///     Represents a function call result
/// </summary>
public class FunctionResult
{
    public FunctionResult()
    {
    }

    /// <summary>
    /// </summary>
    /// <param name="name">Name of the function that was called. Can differ from the originally intended function.</param>
    /// <param name="content">A serializable object (e.g. class / dict / anonymous object) that will be serialized into JSON</param>
    public FunctionResult(string name, object? content)
    {
        Name = name;
        Content = content == null ? "{}" : JsonConvert.SerializeObject(content);
    }

    /// <summary>
    /// </summary>
    /// <param name="name">Name of the function that was called. Can differ from the originally intended function.</param>
    /// <param name="content">A serializable object (e.g. class / dict / anonymous object) that will be serialized into JSON</param>
    /// <param name="data">Any data you might want to work with later but not include in the generated chat message</param>
    public FunctionResult(string name, object? content, object? data)
    {
        Name = name;
        Content = content == null ? "{}" : JsonConvert.SerializeObject(content);
        Data = data;
    }

    /// <summary>
    ///     Name of the function used; passtrough
    /// </summary>
    [JsonProperty("name", Required = Required.Always)]
    public string Name { get; set; }

    /// <summary>
    ///     JSON of the function output
    /// </summary>
    [JsonProperty("content", Required = Required.Always)]
    public string Content { get; set; }

    /// <summary>
    ///     A passtrough arbitrary data
    /// </summary>
    [JsonIgnore]
    public object? Data { get; set; }
}

/// <summary>
///     Represents a tool object
/// </summary>
public class Tool
{
    /// <summary>
    ///     Creates a new tool
    /// </summary>
    /// <param name="function"></param>
    public Tool(ToolFunction function)
    {
        Function = function;
    }

    /// <summary>
    ///     Creates a new too
    /// </summary>
    /// <param name="type"></param>
    public Tool(string type)
    {
        Type = type;
    }

    public Tool()
    {
    }

    /// <summary>
    ///     The code interpreter tool used by assistants
    /// </summary>
    public static Tool Retrieval { get; } = new("retrieval");

    /// <summary>
    ///     The code interpreter tool used by assistants
    /// </summary>
    public static Tool CodeInterpreter { get; } = new("code_interpreter");

    /// <summary>
    ///     Type of the tool, should be always "function" for chat, assistants accepts values "code_interpreter" and
    ///     "retrieval"
    /// </summary>
    [JsonProperty("type", Required = Required.Default)]
    public string Type { get; set; } = "function";

    /// <summary>
    ///     Function description
    /// </summary>
    [JsonProperty("function", Required = Required.Default)]
    public ToolFunction? Function { get; set; }

    /// <summary>
    ///     Creates a tool from <see cref="ToolFunction" />
    /// </summary>
    /// <param name="function"></param>
    /// <returns></returns>
    public static implicit operator Tool(ToolFunction function)
    {
        return new Tool(function);
    }
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
public class ToolFunction
{
    private static readonly JsonSerializerSettings serializerSettings = new() { NullValueHandling = NullValueHandling.Ignore };

    /// <summary>
    ///     Create a parameterless function.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="description"></param>
    public ToolFunction(string name, string description)
    {
        Name = name;
        Description = description;
        Parameters = null;
    }
    
    /// <summary>
    ///     Create a function with parameters.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="description"></param>
    /// <param name="parameters">JSON serialized object, will be deserialized into <see cref="JObject" /> </param>
    public ToolFunction(string name, string description, string parameters)
    {
        Name = name;
        Description = description;
        Parameters = JObject.Parse(parameters);
    }

    /// <summary>
    ///     Create a function with parameters.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="description"></param>
    /// <param name="parameters"></param>
    public ToolFunction(string name, string description, JObject parameters)
    {
        Name = name;
        Description = description;
        Parameters = parameters;
    }

    /// <summary>
    ///     Create a function with parameters.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="description"></param>
    /// <param name="parameters">A JSON-serializable object</param>
    public ToolFunction(string name, string description, object parameters)
    {
        Name = name;
        Description = description;
        Parameters = JObject.FromObject(parameters, JsonSerializer.Create(serializerSettings));
    }

    /// <summary>
    ///     Creates an empty Function object.
    /// </summary>
    private ToolFunction()
    {
    }

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
}