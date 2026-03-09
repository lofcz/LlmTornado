using System;
using LlmTornado.Chat;
using Newtonsoft.Json;

namespace LlmTornado.Files;

/// <summary>
///     Represents a single file used with the OpenAI Files endpoint.  Files are used to upload and manage documents that
///     can be used with features like Fine-tuning.
/// </summary>
public class TornadoFile
{
	/// <summary>
	///     This is always "file"
	/// </summary>
	[JsonProperty("object")]
    public string? Object { get; set; } = "file";

	/// <summary>
	///     Unique id for this file, so that it can be referenced in other operations
	/// </summary>
	[JsonProperty("id")]
    public string Id { get; set; }

	/// <summary>
	///     What is the purpose of this file, fine-tune, fine-tune-results, assistants or assistants_output
	/// </summary>
	[JsonProperty("purpose")]
    public RetrievedFilePurpose Purpose { get; set; }

	/// <summary>
	///     The name of the file
	/// </summary>
	[JsonProperty("filename")]
    public string? Name { get; set; }

	/// <summary>
	///     The size of the file in bytes
	/// </summary>
	[JsonProperty("bytes")]
    public long Bytes { get; set; }

	/// <summary>
	///     Timestamp for the creation time of this file
	/// </summary>
	[JsonProperty("created_at")]
    public long CreatedAt { get; set; }

	/// <summary>
	///     When the object is deleted, this attribute is used in the Delete file operation
	/// </summary>
	[JsonProperty("deleted")]
    public bool? Deleted { get; set; }

	/// <summary>
	///     The status of the File (ie when an upload operation was done: "uploaded")
	/// </summary>
	[JsonProperty("status")]
    public string? Status { get; set; }

	/// <summary>
	///     The status details, it could be null
	/// </summary>
	[JsonProperty("status_details")]
    public string? StatusDetails { get; set; }
	
    /// <summary>
    ///     MIME type. Used only by Google (mimeType) and Mistral (mimetype).
    /// </summary>
    [JsonProperty("mimetype")]
    public string? MimeType { get; set; }
	
	/// <summary>
	///		Whether the content can be downloaded, supported only by Anthropic.
	/// </summary>
	[JsonIgnore]
	public bool Downloadable { get; set; }
	
	/// <summary>
	///     Date the file will be automatically deleted, output only. Used only by Google.
	/// </summary>
	[JsonIgnore]
	public DateTime? ExpirationDate { get; set; }
	
	/// <summary>
	/// URI which can be used for further referencing of the file. Output only, supported by Google.
	/// </summary>
	[JsonIgnore]
	public string? Uri { get; set; }
	
	/// <summary>
	/// State of the file. Used only by Google.
	/// </summary>
	[JsonIgnore]
	public FileLinkStates? State { get; set; }

    /// <summary>
    /// Download URL for the file. Returned by MiniMax, typically valid for 1 hour.
    /// </summary>
    [JsonIgnore]
    public string? DownloadUrl { get; set; }
    
    /// <summary>
    /// Returns <see cref="Uri"/> or <see cref="Id"/> - this property should be used for cross-provider referencing of the file.
    /// </summary>
    public string Reference => Uri ?? Id;
    
    /// <summary>
    /// Number of lines in the file. Returned by Mistral.
    /// </summary>
    [JsonProperty("num_lines")]
    public int? NumLines { get; set; }
    
    /// <summary>
    /// Source of the file (e.g. "upload", "mistral"). Returned by Mistral.
    /// </summary>
    [JsonProperty("source")]
    public TornadoFileSource? Source { get; set; }
    
    /// <summary>
    /// Sample type (e.g. "batch_result"). Returned by Mistral.
    /// </summary>
    [JsonProperty("sample_type")]
    public TornadoFileSampleType? SampleType { get; set; }
    
    /// <summary>
    /// Signature of the server. Returned by Mistral.
    /// </summary>
    [JsonProperty("signature")]
    public string? Signature { get; set; }
}