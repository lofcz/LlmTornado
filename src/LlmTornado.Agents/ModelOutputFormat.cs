namespace LlmTornado.Agents
{
    /// <summary>
    /// Used to define the output format of a model for structured outputs.
    /// </summary>
    public class ModelOutputFormat
    {
        /// <summary>
        /// Name of the Format
        /// </summary>
        public string JsonSchemaFormatName { get; set; }
        /// <summary>
        /// Json Data in binary of the Format Schema
        /// </summary>
        public BinaryData JsonSchema { get; set; }
        /// <summary>
        /// Define if every Input is required default = true
        /// </summary>
        public bool JsonSchemaIsStrict { get; set; } = true;

        /// <summary>
        /// Description of the Format from attribute
        /// </summary>
        public string FormatDescription { get; set; }

        public ModelOutputFormat() { }

        public ModelOutputFormat(string jsonSchemaFormatName, BinaryData jsonSchema, bool jsonSchemaIsStrict, string formatDescription = "")
        {
            JsonSchemaFormatName = jsonSchemaFormatName;
            JsonSchema = jsonSchema;
            JsonSchemaIsStrict = jsonSchemaIsStrict;
            FormatDescription = formatDescription;
        }   
    }
}
