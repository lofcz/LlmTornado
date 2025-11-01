using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LlmTornado.Tests;

public class VectorDbEntry
{
    [JsonProperty("Id")]
    public string Id { get; set; }
    [JsonProperty("Document")]
    public string Document { get; set; }
    [JsonProperty("Metadata")]
    public Dictionary<string, object>? Metadata { get; set; }
    [JsonProperty("Embedding")]
    public float[] Embedding { get; set; }
    [JsonProperty("Distance")]
    public float? Distance { get; set; }

}

[JsonConverter(typeof(VectorDbDataConverter))]
public class VectorDbCollection
{
    [JsonProperty("nextId")]
    public int NextId { get; set; }

    [JsonProperty("entries")]
    public VectorDbEntry[] Entries { get; set; }
}

public class VectorDbDataConverter : JsonConverter<VectorDbCollection>
{
    public override VectorDbCollection ReadJson(JsonReader reader, Type objectType, VectorDbCollection existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        var obj = JObject.Load(reader);

        var result = new VectorDbCollection
        {
            NextId = obj["nextId"]?.ToObject<int>() ?? 0,
            Entries = Array.Empty<VectorDbEntry>()
        };

        var entriesToken = obj["entries"];
        if (entriesToken is JObject entriesObj)
        {
            // Order by numeric key (e.g., "0","1","2",...) to keep a stable order
            var ordered = entriesObj.Properties()
                                     .OrderBy(p => int.TryParse(p.Name, out var n) ? n : int.MaxValue);

            var list = new List<VectorDbEntry>();
            foreach (var prop in ordered)
            {
                // Deserialize each entry using the default serializer
                var entry = prop.Value.ToObject<VectorDbEntry>(serializer);
                if (entry != null)
                    list.Add(entry);
            }

            result.Entries = list.ToArray();
        }

        return result;
    }

    public override void WriteJson(JsonWriter writer, VectorDbCollection value, JsonSerializer serializer)
    {
        // Serialize back to the original shape: { nextId, entries: { "0": {...}, "1": {...}, ... } }
        writer.WriteStartObject();

        writer.WritePropertyName("nextId");
        writer.WriteValue(value.NextId);

        writer.WritePropertyName("entries");
        writer.WriteStartObject();
        if (value.Entries != null)
        {
            for (int i = 0; i < value.Entries.Length; i++)
            {
                writer.WritePropertyName(i.ToString());
                serializer.Serialize(writer, value.Entries[i]);
            }
        }
        writer.WriteEndObject();

        writer.WriteEndObject();
    }
}
