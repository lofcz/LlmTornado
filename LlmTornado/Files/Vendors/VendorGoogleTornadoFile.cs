using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using LlmTornado.Chat;
using Newtonsoft.Json;

namespace LlmTornado.Files.Vendors;

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
        
    /// <summary>
    /// STATE_UNSPECIFIED
    /// PROCESSING
    /// ACTIVE
    /// FAILED
    /// </summary>
    [JsonProperty("state")]
    public string State { get; set; }
        
    [JsonProperty("Source")]
    public string Source { get; set; }

    private static readonly FrozenDictionary<string, FileLinkStates> statesMap = new Dictionary<string, FileLinkStates>
    {
        { "STATE_UNSPECIFIED", FileLinkStates.Unknown },
        { "PROCESSING", FileLinkStates.Processing },
        { "ACTIVE", FileLinkStates.Active },
        { "FAILED", FileLinkStates.Failed }
    }.ToFrozenDictionary();
    
    public TornadoFile ToFile(string? postData)
    {
        return new TornadoFile
        {
            Id = Name,
            MimeType = MimeType,
            ExpirationDate = ExpirationTime,
            Uri = Uri,
            State = statesMap.GetValueOrDefault(State, FileLinkStates.Unknown)
        };
    }
}

internal class VendorGoogleTornadoFile
{
    [JsonProperty("file")]
    public VendorGoogleTornadoFileContent File { get; set; }
    
    public TornadoFile ToFile(string? postData)
    {
        return File.ToFile(postData);
    }
}