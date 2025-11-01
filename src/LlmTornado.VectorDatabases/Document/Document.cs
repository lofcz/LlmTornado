using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace LlmTornado.VectorDatabases;

public class Document
{
    [JsonProperty("id")]
    public string Id { get; set; }

    [JsonProperty("content")]
    public string Content { get; set; }

    [JsonProperty("metadata")]
    public Dictionary<string, object> Metadata { get; set; }

    public Document(string id, string content, Dictionary<string, object> metadata = null)
    {
        Id = id;
        Content = content;
        Metadata = metadata ?? new Dictionary<string, object>();
    }

    public static implicit operator VectorDocument(Document doc)
    {
        return new VectorDocument(doc.Id, doc.Content, doc.Metadata, null);
    }

    public static implicit operator Document(VectorDocument vdoc)
    {
        return new Document(vdoc.Id, vdoc.Content, vdoc.Metadata);
    }
}
