using System;
using System.Globalization;
using LlmTornado.Chat;
using Newtonsoft.Json;

namespace LlmTornado.Files.Vendors.Anthropic;

internal class VendorAnthropicTornadoFile
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
	///     The name of the file
	/// </summary>
	[JsonProperty("filename")]
    public string Name { get; set; }

	/// <summary>
	///     The size of the file in bytes
	/// </summary>
	[JsonProperty("size_bytes")]
    public long SizeBytes { get; set; }

	/// <summary>
	///     Timestamp for the creation time of this file
	/// </summary>
	[JsonProperty("created_at")]
    public string CreatedAt { get; set; }

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
	///     MIME type, output only. Used only by Google.
	/// </summary>
	[JsonProperty("mime_type")]
	public string? MimeType { get; set; }
	
	[JsonProperty("downloadable")]
	public bool Downloadable { get; set; }

	public TornadoFile ToFile()
	{
		long createdAt = 0;
		
		if (DateTimeOffset.TryParse(CreatedAt, null, DateTimeStyles.RoundtripKind, out DateTimeOffset dateTimeOffset))
		{
			createdAt = dateTimeOffset.ToUnixTimeSeconds();
		}

		return new TornadoFile
		{
			CreatedAt = createdAt,
			Id = Id,
			MimeType = MimeType,
			Bytes = SizeBytes,
			Object = "file"
		};
	}
}