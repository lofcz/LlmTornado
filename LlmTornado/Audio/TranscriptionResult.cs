﻿using System.Collections.Generic;
using LlmTornado;
using Newtonsoft.Json;

namespace LlmTornado.Audio;

/// <summary>
///     Transcription format for vtt results.
/// </summary>
public class TranscriptionResult : ApiResultBase
{
    /// <summary>
    ///     Text of the transcript result.
    /// </summary>
    public string Text { get; set; }
    
    /// <summary>
    ///     Task type. Translate or transcript.
    /// </summary>
    [JsonProperty("task")]
    public string? Task { get; set; }

    /// <summary>
    ///     Language of the audio.
    /// </summary>
    [JsonProperty("language")]
    public string? Language { get; set; }

    /// <summary>
    ///     Audio duration.
    /// </summary>
    [JsonProperty("duration")]
    public float Duration { get; set; }

    /// <summary>
    ///     Audio segments.
    /// </summary>
    [JsonProperty("segments")]
    public List<TranscriptionSegment> Segments { get; set; } = [];
}

/// <summary>
///     Segment of the transcript.
/// </summary>
public class TranscriptionSegment
{
    /// <summary>
    ///     Segment id
    /// </summary>
    [JsonProperty("id")]
    public int Id { get; set; }

    /// <summary>
    ///     Start time.
    /// </summary>
    [JsonProperty("start")]
    public float Start { get; set; }

    /// <summary>
    ///     End time.
    /// </summary>
    [JsonProperty("end")]
    public float End { get; set; }

    /// <summary>
    ///     Segment text.
    /// </summary>
    [JsonProperty("text")]
    public string Text { get; set; }

    /// <summary>
    ///     Text tokens.
    /// </summary>
    [JsonProperty("tokens")]
    public int[] Tokens { get; set; }

    /// <summary>
    ///     Temperature.
    /// </summary>
    [JsonProperty("temperature")]
    public double Temperature { get; set; }

    /// <summary>
    ///     Average log probabilities of the text.
    /// </summary>
    [JsonProperty("avg_logprob")]
    public double AvgLogProb { get; set; }

    /// <summary>
    ///     Compression ratio.
    /// </summary>
    [JsonProperty("compression_ratio")]
    public double CompressionRation { get; set; }

    /// <summary>
    ///     No speech probability.
    /// </summary>
    [JsonProperty("no_speech_prob")]
    public double NoSpeechProb { get; set; }

    /// <summary>
    ///     Transient.
    /// </summary>
    [JsonProperty("transient")]
    public bool Transient { get; set; }
}