using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace LlmTornado.Uploads;

/// <summary>
///     Status of the upload.
/// </summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum UploadStatus
{
    /// <summary>
    ///     Pending upload.
    /// </summary>
    [EnumMember(Value = "pending")]
    Pending,

    /// <summary>
    ///     Completed upload.
    /// </summary>
    [EnumMember(Value = "completed")]
    Completed,

    /// <summary>
    ///     Cancelled upload.
    /// </summary>
    [EnumMember(Value = "cancelled")]
    Cancelled,

    /// <summary>
    ///     Expired upload.
    /// </summary>
    [EnumMember(Value = "expired")]
    Expired
}

/// <summary>
///     Purpose of the uploaded file.
/// </summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum UploadPurpose
{
    /// <summary>
    ///     Used in the Assistants API.
    /// </summary>
    [EnumMember(Value = "assistants")]
    Assistants,

    /// <summary>
    ///     Used in the Batch API.
    /// </summary>
    [EnumMember(Value = "batch")]
    Batch,

    /// <summary>
    ///     Used for fine-tuning.
    /// </summary>
    [EnumMember(Value = "fine-tune")]
    FineTune,

    /// <summary>
    ///     Images used for vision fine-tuning.
    /// </summary>
    [EnumMember(Value = "vision")]
    Vision,

    /// <summary>
    ///     Flexible file type for any purpose.
    /// </summary>
    [EnumMember(Value = "user_data")]
    UserData,

    /// <summary>
    ///     Used for evaluation data sets.
    /// </summary>
    [EnumMember(Value = "evals")]
    Evals
} 