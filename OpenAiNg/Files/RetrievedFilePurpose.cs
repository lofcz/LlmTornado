using System;
using Newtonsoft.Json;

namespace OpenAiNg.Files;

/// <summary>
///     Represents the retrieved purpose of a file
/// </summary>
public class RetrievedFilePurpose
{
    private RetrievedFilePurpose(string? value)
    {
        Value = value;
    }

    public string Value { get; }

    /// <summary>
    ///     Finetuning
    /// </summary>
    public static RetrievedFilePurpose Finetune => new("fine-tune");

    /// <summary>
    ///     Finetuning results
    /// </summary>
    public static RetrievedFilePurpose FinetuneResults => new("fine-tune-results");

    /// <summary>
    ///     Assistants input file
    /// </summary>
    public static RetrievedFilePurpose Assistants => new("assistants");

    /// <summary>
    ///     Assistants output file
    /// </summary>
    public static RetrievedFilePurpose AssistantsOutput => new("assistants_output");

    /// <summary>
    ///     Converts <see cref="FilePurpose" /> into <see cref="RetrievedFilePurpose" />
    /// </summary>
    /// <param name="purpose"></param>
    /// <returns></returns>
    public static RetrievedFilePurpose ToRetrievedFilePurpose(FilePurpose purpose)
    {
        return purpose is FilePurpose.Assistants ? Assistants : Finetune;
    }

    /// <summary>
    ///     Gets the string value for this file purpose to pass to the API
    /// </summary>
    /// <returns>The quality as a string</returns>
    public override string ToString()
    {
        return Value;
    }

    /// <summary>
    ///     Gets the string value for this file purpose to pass to the API
    /// </summary>
    /// <param name="value">The RetrievedFilePurpose to convert</param>
    public static implicit operator string(RetrievedFilePurpose value)
    {
        return value.Value;
    }

    internal class RetrievedFilePurposeJsonConverter : JsonConverter<RetrievedFilePurpose>
    {
        public override void WriteJson(JsonWriter writer, RetrievedFilePurpose value, JsonSerializer serializer)
        {
            writer.WriteValue(value.ToString());
        }

        public override RetrievedFilePurpose ReadJson(JsonReader reader, Type objectType, RetrievedFilePurpose existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType is JsonToken.String)
            {
                string? str = reader.Value as string;
                return new RetrievedFilePurpose(str);
            }

            return new RetrievedFilePurpose(reader.ReadAsString());
        }
    }
}