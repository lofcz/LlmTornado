
namespace LlmTornado.Agents
{
    /// <summary>
    /// State of the model response Item
    /// </summary>
    public enum ModelStatus
    {
        InProgress,
        Completed,
        Incomplete
    }

    /// <summary>
    /// Base Class for the input/output items
    /// </summary>
    public class ModelItem
    {
        /// <summary>
        /// Id of the Item
        /// </summary>
        public string Id { get; set; }
        public ModelItem(string id) { 
            Id = id;
        }
    }

    /// <summary>
    /// Base class for the CallItems that require a returned message
    /// </summary>
    public class CallItem : ModelItem
    {
        /// <summary>
        /// Call ID to respond to
        /// </summary>
        public string CallId { get; set; }
        public CallItem(string id, string callId) :base(id)
        {
            Id = id;
            CallId = callId;
        }
    }

    /// <summary>
    /// Web Searching enum 
    /// </summary>
    public enum ModelWebSearchingStatus
    {
        InProgress,
        Searching,
        Completed,
        Failed
    }

    /// <summary>
    /// Web Call item.. used to keep track.. no data stored yet.
    /// </summary>
    public class ModelWebCallItem : ModelItem
    {
        /// <summary>
        /// Search Queries performed
        /// </summary>
        private string query = "";
        /// <summary>
        /// Searching status
        /// </summary>
        public ModelWebSearchingStatus Status { get; set; }
        public string Query { get => query; set => query = value; }

        public ModelWebCallItem(string id, ModelWebSearchingStatus status) : base(id)
        {
            Id = id;
            Status = status;
        }
    }

    /// <summary>
    /// reasoning response
    /// </summary>
    public class ModelReasoningItem : ModelItem
    {
        public string? EncryptedContent { get; set; } = "";
        public List<string> Summary { get; set; } = new List<string>();
        public ModelReasoningItem(string id, string[]? summary = null) : base(id)
        {
            Id = id;
            Summary = summary == null ? Summary : [.. summary];
        }
    }

    /// <summary>
    /// File search call content recieved
    /// </summary>
    public class FileSearchCallContent
    {
        public FileSearchCallContent()
        {
        }

        internal FileSearchCallContent(string fileId, string text, string filename, float? score)
        {
            FileId = fileId;
            Text = text;
            Filename = filename;
            Score = score;
        }
        /// <summary>
        /// File searched
        /// </summary>
        public string FileId { get; set; }

        /// <summary>
        /// Text returned
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// Name of the file
        /// </summary>
        public string Filename { get; set; }

        /// <summary>
        /// distance of the cos result to query
        /// </summary>
        public float? Score { get; set; }
    }

    /// <summary>
    /// File Search item
    /// </summary>
    public class ModelFileSearchCallItem : ModelItem
    {
        /// <summary>
        /// Query performed on text
        /// </summary>
        public List<string> Queries { get; set; } = new List<string>();

        /// <summary>
        /// Status of the search
        /// </summary>
        public ModelStatus Status { get; set; }

        /// <summary>
        /// Texts from the file searched 
        /// </summary>
        public List<FileSearchCallContent> Results { get; set; } = new List<FileSearchCallContent>();

        public ModelFileSearchCallItem(string id, List<string> queries, ModelStatus status, List<FileSearchCallContent> results) : base(id)
        {
            Id = id;
            Queries = queries ?? throw new ArgumentNullException(nameof(queries), "Queries cannot be null");
            Status = status;
            Results = results;
        }
    }

    /// <summary>
    /// Function Call item to call functions
    /// </summary>
    public class ModelFunctionCallItem : CallItem
    {
        /// <summary>
        /// Call Id to return too
        /// </summary>
        public string CallId { get; set; }
        /// <summary>
        /// Function being called
        /// </summary>
        public string FunctionName { get; set; }
        /// <summary>
        /// Arguments to run in the functions
        /// </summary>
        public BinaryData? FunctionArguments { get; set; }
        /// <summary>
        /// Status of the function call
        /// </summary>
        public ModelStatus Status { get; set; }
        public ModelFunctionCallItem(string id, string callId, string functionName, ModelStatus status, BinaryData? functionArguments = null) : base(id, callId)
        {
            CallId = callId;
            Id = id;
            Status = status;
            FunctionName = functionName;
            FunctionArguments = functionArguments;
        }
    }

    /// <summary>
    /// Response to the function call
    /// </summary>
    public class ModelFunctionCallOutputItem : CallItem
    {
        /// <summary>
        /// CallID to respond to
        /// </summary>
        public string CallId { get; set; }
        /// <summary>
        /// Result of the function as text
        /// </summary>
        public string FunctionOutput { get; set; }
        /// <summary>
        /// Name of the function being called
        /// </summary>
        public string FunctionName { get; set; }
        /// <summary>
        /// Status of the function result (always complete)
        /// </summary>
        public ModelStatus Status { get; set; }
        public ModelFunctionCallOutputItem(string id, string callId, string functionOutput, ModelStatus status, string functionName) : base(id, callId)
        {
            CallId = callId;
            Id = id;
            Status = status;
            FunctionOutput = functionOutput;
            FunctionName = functionName;
        }
    }


    /// <summary>
    /// Message Item to hold message content like text and images
    /// </summary>
    public class ModelMessageItem : ModelItem
    {
        /// <summary>
        /// Contents of the message (text/images)
        /// </summary>
        public List<ModelMessageContent> Content { get; set; } = new List<ModelMessageContent>();
        /// <summary>
        /// Owner of the messages
        /// </summary>
        public string Role { get; set; }
        /// <summary>
        /// Status of the message
        /// </summary>
        public ModelStatus Status { get; set; }
        public ModelMessageItem(string id, string role, List<ModelMessageContent> content, ModelStatus status) :base(id)
        {
            Id = id;
            Status = status;
            Role = role;
            Content = content;
        }
        /// <summary>
        /// Convert the last message if it is Text into text
        /// </summary>
        public string? Text => ((ModelMessageTextContent?)Content.LastOrDefault(mess => mess is ModelMessageTextContent))?.Text ?? "";
    }
}
