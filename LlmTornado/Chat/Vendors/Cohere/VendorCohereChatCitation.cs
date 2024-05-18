using System.Collections.Generic;
using Argon;

namespace LlmTornado.Chat.Vendors.Cohere;

/// <summary>
///     Cohere citation.
/// </summary>
public class VendorCohereChatCitation
{
    /// <summary>
    ///     Index of the character in response where the citation starts.
    /// </summary>
    [JsonProperty("start")]
    public int Start { get; set; }
    
    /// <summary>
    ///     Index of the character in response where the citation ends.
    /// </summary>
    [JsonProperty("end")]
    public int End { get; set; }

    /// <summary>
    ///     Length of the citation in characters.
    /// </summary>
    [JsonIgnore]
    public int Length => End - Start;
    
    /// <summary>
    ///     Text of the citation.
    /// </summary>
    [JsonProperty("text")]
    public string Text { get; set; }
    
    /// <summary>
    ///     Document Ids, see <see cref="VendorCohereChatResult.Documents"/>
    /// </summary>
    [JsonProperty("document_ids")]
    public List<string> DocumentIds { get; set; }
}