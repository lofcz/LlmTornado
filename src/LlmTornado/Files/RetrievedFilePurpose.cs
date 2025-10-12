using System;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace LlmTornado.Files;

/// <summary>
///     Represents the retrieved purpose of a file
/// </summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum RetrievedFilePurpose
{
    /// <summary>
    ///     Finetuning
    /// </summary>
    [EnumMember(Value = "fine-tune")]
    Finetune,

    /// <summary>
    ///     Finetuning results
    /// </summary>
    [EnumMember(Value = "fine-tune-results")]
    FinetuneResults,

    /// <summary>
    ///     Assistants input file
    /// </summary>
    [EnumMember(Value = "assistants")]
    Assistants,

    /// <summary>
    ///     Assistants output file
    /// </summary>
    [EnumMember(Value = "assistants_output")]
    AssistantsOutput,
    
    /// <summary>
    ///     User data.
    /// </summary>
    [EnumMember(Value = "user_data")]
    UserData,

    /// <summary>
    ///     Agent file for ZAI
    /// </summary>
    [EnumMember(Value = "agent")]
    Agent
}

/// <summary>
///     Extension methods for RetrievedFilePurpose enum
/// </summary>
public static class RetrievedFilePurposeExtensions
{
    /// <summary>
    ///     Converts <see cref="FilePurpose" /> into <see cref="RetrievedFilePurpose" />
    /// </summary>
    /// <param name="purpose"></param>
    /// <returns></returns>
    public static RetrievedFilePurpose ToRetrievedFilePurpose(this FilePurpose purpose)
    {
        return purpose switch
        {
            FilePurpose.Assistants => RetrievedFilePurpose.Assistants,
            FilePurpose.Agent => RetrievedFilePurpose.Agent,
            _ => RetrievedFilePurpose.Finetune
        };
    }
}