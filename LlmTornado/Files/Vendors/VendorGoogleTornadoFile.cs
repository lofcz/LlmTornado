using System;
using Newtonsoft.Json;

namespace LlmTornado.Files.Vendors;

internal class VendorGoogleTornadoFile
{
    internal class VendorGoogleTornadoFileContent
    {
        [JsonProperty("name")]
        public string Name { get; set; }
        
        [JsonProperty("mimeType")]
        public string MimeType { get; set; }
        
        [JsonProperty("sizeBytes")]
        public long SizeBytes { get; set; }
        
        [JsonProperty("createTime")]
        public DateTime CreateTime { get; set; }
        
        [JsonProperty("updateTime")]
        public DateTime UpdateTime { get; set; }
        
        [JsonProperty("expirationTime")]
        public DateTime ExpirationTime { get; set; }
        
        [JsonProperty("sha256Hash")]
        public string Sha256hash { get; set; }
        
        [JsonProperty("uri")]
        public string Uri { get; set; }
        
        [JsonProperty("state")]
        public string State { get; set; }
        
        [JsonProperty("Source")]
        public string Source { get; set; }
        
        public TornadoFile ToFile(string? postData)
        {
            return new TornadoFile
            {
                Id = Name,
                MimeType = MimeType,
                ExpirationDate = ExpirationTime
            };
        }
    }
    
    [JsonProperty("file")]
    public VendorGoogleTornadoFileContent File { get; set; }
    
    public TornadoFile ToFile(string? postData)
    {
        return File.ToFile(postData);
    }
}