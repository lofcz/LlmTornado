using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using LlmTornado.Audio.Models;
using LlmTornado.Realtime.Translation;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LlmTornado.Realtime.Vendors.OpenAi;

internal static class VendorOpenAiRealtimeTranslation
{
    internal const int DefaultSampleRate = 24_000;
    internal const int FrameDurationMs = 200;
    internal const int FrameByteSize = DefaultSampleRate * 2 * FrameDurationMs / 1000;

    private static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
    {
        NullValueHandling = NullValueHandling.Ignore
    };

    private static readonly FrozenDictionary<string, RealtimeTranslationEventTypes> TypeMap =
        new Dictionary<string, RealtimeTranslationEventTypes>
        {
            ["session.created"] = RealtimeTranslationEventTypes.SessionCreated,
            ["session.updated"] = RealtimeTranslationEventTypes.SessionUpdated,
            ["session.closed"] = RealtimeTranslationEventTypes.SessionClosed,
            ["session.input_transcript.delta"] = RealtimeTranslationEventTypes.InputTranscriptDelta,
            ["session.output_transcript.delta"] = RealtimeTranslationEventTypes.OutputTranscriptDelta,
            ["session.output_audio.delta"] = RealtimeTranslationEventTypes.OutputAudioDelta,
            ["error"] = RealtimeTranslationEventTypes.Error
        }.ToFrozenDictionary();

    internal static string BuildWebSocketUrl(string apiVersion, string modelName, string? apiUrlFormat)
    {
        string httpBase = apiUrlFormat is not null
            ? string.Format(apiUrlFormat, apiVersion, "realtime/translations")
            : $"https://api.openai.com/{apiVersion}/realtime/translations";

        Uri uri = new Uri(httpBase);
        string wsScheme = uri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase) ? "wss" : "ws";
        UriBuilder builder = new UriBuilder(uri)
        {
            Scheme = wsScheme,
            Port = uri.IsDefaultPort ? -1 : uri.Port,
            Query = $"model={Uri.EscapeDataString(modelName)}"
        };

        return builder.Uri.ToString();
    }

    internal static string SerializeSessionUpdate(RealtimeTranslationSessionConfig config)
    {
        JObject session = new JObject
        {
            ["audio"] = BuildAudioConfig(config)
        };

        return JsonConvert.SerializeObject(new
        {
            type = "session.update",
            session
        }, JsonSettings);
    }

    internal static string SerializeAppendAudio(string base64Audio, string? eventId = null)
    {
        if (eventId is null)
        {
            return JsonConvert.SerializeObject(new
            {
                type = "session.input_audio_buffer.append",
                audio = base64Audio
            }, JsonSettings);
        }

        return JsonConvert.SerializeObject(new
        {
            type = "session.input_audio_buffer.append",
            audio = base64Audio,
            event_id = eventId
        }, JsonSettings);
    }

    internal static string SerializeClose(string? eventId = null)
    {
        if (eventId is null)
        {
            return JsonConvert.SerializeObject(new { type = "session.close" }, JsonSettings);
        }

        return JsonConvert.SerializeObject(new
        {
            type = "session.close",
            event_id = eventId
        }, JsonSettings);
    }

    internal static RealtimeTranslationEvent ParseEvent(string json)
    {
        JObject root = JObject.Parse(json);
        string type = root.Value<string>("type") ?? string.Empty;
        RealtimeTranslationEventTypes eventType = TypeMap.GetValueOrDefault(type, RealtimeTranslationEventTypes.Unknown);

        RealtimeTranslationEvent evt = new RealtimeTranslationEvent
        {
            Type = type,
            EventType = eventType,
            EventId = root.Value<string>("event_id"),
            Delta = root.Value<string>("delta"),
            ElapsedMs = root.Value<int?>("elapsed_ms"),
            AudioFormat = root.Value<string>("format"),
            SampleRate = root.Value<int?>("sample_rate"),
            Channels = root.Value<int?>("channels"),
            RawJson = json
        };

        if (eventType is RealtimeTranslationEventTypes.OutputAudioDelta && evt.Delta is not null)
        {
            evt.AudioData = Convert.FromBase64String(evt.Delta);
        }

        if (root["session"] is JObject sessionObj)
        {
            evt.Session = ParseSession(sessionObj);
        }

        if (root["error"] is JObject errorObj)
        {
            evt.Error = new RealtimeTranslationError
            {
                Message = errorObj.Value<string>("message"),
                Type = errorObj.Value<string>("type"),
                Code = errorObj.Value<string>("code"),
                Param = errorObj.Value<string>("param"),
                EventId = errorObj.Value<string>("event_id")
            };
        }

        return evt;
    }

    private static JObject BuildAudioConfig(RealtimeTranslationSessionConfig config)
    {
        JObject audio = new JObject();
        JObject input = new JObject();
        JObject output = new JObject();
        bool hasInput;
        bool hasOutput;

        if (config.InputTranscriptionModel is not null)
        {
            input["transcription"] = new JObject
            {
                ["model"] = config.InputTranscriptionModel.Name
            };
            hasInput = true;
        }
        else
        {
            hasInput = false;
        }

        if (config.NoiseReduction is not null)
        {
            input["noise_reduction"] = new JObject
            {
                ["type"] = config.NoiseReduction switch
                {
                    RealtimeTranslationNoiseReduction.NearField => "near_field",
                    RealtimeTranslationNoiseReduction.FarField => "far_field",
                    _ => "near_field"
                }
            };
            hasInput = true;
        }

        if (!string.IsNullOrWhiteSpace(config.OutputLanguage))
        {
            output["language"] = config.OutputLanguage;
            hasOutput = true;
        }
        else
        {
            hasOutput = false;
        }

        if (hasInput)
        {
            audio["input"] = input;
        }

        if (hasOutput)
        {
            audio["output"] = output;
        }

        return audio;
    }

    private static RealtimeTranslationSessionState ParseSession(JObject sessionObj)
    {
        RealtimeTranslationSessionState session = new RealtimeTranslationSessionState
        {
            Id = sessionObj.Value<string>("id"),
            Model = sessionObj.Value<string>("model"),
            SessionType = sessionObj.Value<string>("type"),
            ExpiresAt = sessionObj.Value<long?>("expires_at")
        };

        if (sessionObj["audio"]?["output"]?["language"] is JValue outputLanguage)
        {
            session.OutputLanguage = outputLanguage.Value<string>();
        }

        if (sessionObj["audio"]?["input"]?["transcription"]?["model"] is JValue transcriptionModel)
        {
            session.InputTranscriptionModel = transcriptionModel.Value<string>();
        }

        if (sessionObj["audio"]?["input"]?["noise_reduction"]?["type"] is JValue noiseReduction)
        {
            session.NoiseReduction = noiseReduction.Value<string>();
        }

        return session;
    }
}
