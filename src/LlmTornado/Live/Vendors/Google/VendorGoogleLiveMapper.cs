using System;
using System.Collections.Generic;
using System.Linq;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Chat.Vendors.Google;
using LlmTornado.Code;
using LlmTornado.Common;
using LlmTornado.ChatFunctions;
using Newtonsoft.Json;

namespace LlmTornado.Live.Vendors.Google;

internal static class VendorGoogleLiveMapper
{
    internal static VendorGoogleLiveSetup ToSetup(LiveSessionConfig config)
    {
        ChatModel model = config.Model ?? ChatModelGoogleGeminiPreview.ModelGemini31FlashLivePreview;

        VendorGoogleLiveGenerationConfig generationConfig = new VendorGoogleLiveGenerationConfig
        {
            ResponseModalities = config.ResponseModalities?.Select(MapModality).Where(x => x is not null).Cast<string>().ToList(),
            MaxOutputTokens = config.MaxOutputTokens,
            MediaResolution = MapMediaResolution(config.MediaResolution),
            ThinkingConfig = BuildThinkingConfig(config),
            SpeechConfig = string.IsNullOrWhiteSpace(config.VoiceName)
                ? null
                : new VendorGoogleLiveSpeechConfig
                {
                    VoiceConfig = new VendorGoogleLiveVoiceConfig
                    {
                        PrebuiltVoiceConfig = new VendorGoogleLivePrebuiltVoiceConfig
                        {
                            VoiceName = config.VoiceName
                        }
                    }
                }
        };

        VendorGoogleLiveSetup setup = new VendorGoogleLiveSetup
        {
            Model = model.Name.StartsWith("models/", StringComparison.Ordinal) ? model.Name : $"models/{model.Name}",
            GenerationConfig = generationConfig,
            SystemInstruction = string.IsNullOrWhiteSpace(config.SystemInstruction)
                ? null
                : new VendorGoogleLiveContent
                {
                    Parts = [new VendorGoogleLivePart { Text = config.SystemInstruction }]
                },
            RealtimeInputConfig = MapRealtimeInputConfig(config.RealtimeInput),
            HistoryConfig = config.History is null
                ? null
                : new VendorGoogleLiveHistoryConfig
                {
                    InitialHistoryInClientContent = config.History.InitialHistoryInClientContent ? true : null
                },
            SessionResumption = config.SessionResumption?.Handle is null
                ? null
                : new VendorGoogleLiveSessionResumptionConfig { Handle = config.SessionResumption.Handle },
            ContextWindowCompression = MapContextWindowCompression(config.ContextWindowCompression),
            InputAudioTranscription = config.InputAudioTranscription ? new { } : null,
            OutputAudioTranscription = config.OutputAudioTranscription ? new { } : null
        };

        if (config.Tools is { Count: > 0 })
        {
            var tools = VendorGoogleChatRequest.GetToolsAndToolChoice(null, config.Tools, null);
            if (tools.Item1 is { Count: > 0 })
            {
                setup.Tools = tools.Item1.Cast<object>().ToList();
            }
        }

        return setup;
    }

    internal static VendorGoogleLiveClientContent ToClientContent(LiveClientContent content)
    {
        return new VendorGoogleLiveClientContent
        {
            Turns = content.Turns?.Select(ToContent).ToList(),
            TurnComplete = content.TurnComplete
        };
    }

    internal static VendorGoogleLiveRealtimeInput ToRealtimeInput(LiveRealtimeInput input)
    {
        VendorGoogleLiveRealtimeInput wire = new VendorGoogleLiveRealtimeInput
        {
            Text = input.Text,
            AudioStreamEnd = input.AudioStreamEnd ? true : null,
            ActivityStart = input.ActivityStart ? new { } : null,
            ActivityEnd = input.ActivityEnd ? new { } : null
        };

        if (input.Audio is { Length: > 0 })
        {
            wire.Audio = new VendorGoogleLiveBlob
            {
                MimeType = input.AudioMimeType,
                Data = Convert.ToBase64String(input.Audio)
            };
        }

        if (input.Video is { Length: > 0 })
        {
            wire.Video = new VendorGoogleLiveBlob
            {
                MimeType = input.VideoMimeType,
                Data = Convert.ToBase64String(input.Video)
            };
        }

        return wire;
    }

    internal static VendorGoogleLiveToolResponse ToToolResponse(LiveToolResponse response)
    {
        return new VendorGoogleLiveToolResponse
        {
            FunctionResponses = response.FunctionResponses?.Select(fr => new VendorGoogleLiveFunctionResponse
            {
                Id = fr.Id,
                Name = fr.Name,
                Response = fr.Response
            }).ToList()
        };
    }

    internal static LiveServerMessage ToServerMessage(string rawJson, VendorGoogleLiveServerEnvelope envelope)
    {
        LiveServerMessage message = new LiveServerMessage
        {
            RawJson = rawJson,
            SetupComplete = envelope.SetupComplete is not null,
            UsageMetadata = envelope.UsageMetadata is null
                ? null
                : new LiveUsageMetadata
                {
                    PromptTokenCount = envelope.UsageMetadata.PromptTokenCount,
                    ResponseTokenCount = envelope.UsageMetadata.ResponseTokenCount,
                    ThoughtsTokenCount = envelope.UsageMetadata.ThoughtsTokenCount,
                    TotalTokenCount = envelope.UsageMetadata.TotalTokenCount
                }
        };

        if (envelope.ServerContent is not null)
        {
            message.ServerContent = new LiveServerContent
            {
                GenerationComplete = envelope.ServerContent.GenerationComplete ?? false,
                TurnComplete = envelope.ServerContent.TurnComplete ?? false,
                Interrupted = envelope.ServerContent.Interrupted ?? false,
                ModelTurn = envelope.ServerContent.ModelTurn is null ? null : FromContent(envelope.ServerContent.ModelTurn),
                InputTranscription = MapTranscription(envelope.ServerContent.InputTranscription),
                OutputTranscription = MapTranscription(envelope.ServerContent.OutputTranscription)
            };
        }

        if (envelope.ToolCall?.FunctionCalls is not null)
        {
            message.ToolCall = new LiveToolCall
            {
                FunctionCalls = envelope.ToolCall.FunctionCalls.Select(fc =>
                {
                    FunctionCall call = new FunctionCall
                    {
                        Name = fc.Name ?? string.Empty,
                        Arguments = fc.Args is string s ? s : JsonConvert.SerializeObject(fc.Args ?? new { })
                    };

                    if (fc.Id is not null)
                    {
                        call.ToolCall = new ToolCall { Id = fc.Id };
                    }

                    return call;
                }).ToList()
            };
        }

        if (envelope.ToolCallCancellation?.Ids is not null)
        {
            message.ToolCallCancellation = new LiveToolCallCancellation { Ids = envelope.ToolCallCancellation.Ids };
        }

        if (envelope.GoAway is not null)
        {
            message.GoAway = new LiveGoAway { TimeLeft = envelope.GoAway.TimeLeft };
        }

        if (envelope.SessionResumptionUpdate is not null)
        {
            message.SessionResumptionUpdate = new LiveSessionResumptionUpdate
            {
                NewHandle = envelope.SessionResumptionUpdate.NewHandle,
                Resumable = envelope.SessionResumptionUpdate.Resumable ?? false
            };
        }

        return message;
    }

    internal static string BuildWebSocketUrl(string apiVersion, string? apiKey, string? accessToken)
    {
        string service = accessToken is null
            ? $"google.ai.generativelanguage.{apiVersion}.GenerativeService.BidiGenerateContent"
            : $"google.ai.generativelanguage.{apiVersion}.GenerativeService.BidiGenerateContentConstrained";

        string baseUrl = $"wss://generativelanguage.googleapis.com/ws/{service}";

        if (!string.IsNullOrWhiteSpace(accessToken))
        {
            return $"{baseUrl}?access_token={Uri.EscapeDataString(accessToken)}";
        }

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException("Google Live API requires an API key or access token.");
        }

        return $"{baseUrl}?key={Uri.EscapeDataString(apiKey.Trim())}";
    }

    private static VendorGoogleLiveThinkingConfig? BuildThinkingConfig(LiveSessionConfig config)
    {
        if (config.ThinkingLevel is null && config.ThinkingBudget is null && config.IncludeThoughts is null)
        {
            return null;
        }

        return new VendorGoogleLiveThinkingConfig
        {
            ThinkingLevel = MapThinkingLevel(config.ThinkingLevel),
            ThinkingBudget = config.ThinkingBudget,
            IncludeThoughts = config.IncludeThoughts
        };
    }

    private static VendorGoogleLiveContextWindowCompressionConfig? MapContextWindowCompression(LiveContextWindowCompressionConfig? config)
    {
        if (config is null)
        {
            return null;
        }

        return new VendorGoogleLiveContextWindowCompressionConfig
        {
            TriggerTokens = config.TriggerTokens,
            SlidingWindow = config.TargetTokens is null
                ? null
                : new VendorGoogleLiveSlidingWindow { TargetTokens = config.TargetTokens }
        };
    }

    private static VendorGoogleLiveRealtimeInputConfig? MapRealtimeInputConfig(LiveRealtimeInputConfig? config)
    {
        if (config is null)
        {
            return null;
        }

        VendorGoogleLiveRealtimeInputConfig wire = new VendorGoogleLiveRealtimeInputConfig
        {
            ActivityHandling = MapActivityHandling(config.ActivityHandling),
            TurnCoverage = MapTurnCoverage(config.TurnCoverage)
        };

        if (config.AutomaticActivityDetection is not null)
        {
            wire.AutomaticActivityDetection = new VendorGoogleLiveAutomaticActivityDetection
            {
                Disabled = config.AutomaticActivityDetection.Disabled ? true : null,
                StartOfSpeechSensitivity = MapStartSensitivity(config.AutomaticActivityDetection.StartOfSpeechSensitivity),
                EndOfSpeechSensitivity = MapEndSensitivity(config.AutomaticActivityDetection.EndOfSpeechSensitivity),
                PrefixPaddingMs = config.AutomaticActivityDetection.PrefixPaddingMs,
                SilenceDurationMs = config.AutomaticActivityDetection.SilenceDurationMs
            };
        }

        return wire;
    }

    private static VendorGoogleLiveContent ToContent(ChatMessage message)
    {
        string role = message.Role switch
        {
            ChatMessageRoles.Assistant => "model",
            ChatMessageRoles.User => "user",
            _ => message.Role?.ToString()?.ToLowerInvariant() ?? "user"
        };

        List<VendorGoogleLivePart> parts = [];

        if (message.Parts is not null)
        {
            foreach (ChatMessagePart part in message.Parts)
            {
                if (!string.IsNullOrEmpty(part.Text))
                {
                    parts.Add(new VendorGoogleLivePart { Text = part.Text });
                }
            }
        }
        else if (!string.IsNullOrEmpty(message.Content))
        {
            parts.Add(new VendorGoogleLivePart { Text = message.Content });
        }

        return new VendorGoogleLiveContent { Role = role, Parts = parts };
    }

    private static ChatMessage FromContent(VendorGoogleLiveContent content)
    {
        ChatMessage message = new ChatMessage
        {
            Role = content.Role switch
            {
                "model" => ChatMessageRoles.Assistant,
                "user" => ChatMessageRoles.User,
                _ => ChatMessageRoles.Assistant
            },
            Parts = content.Parts?.Select(p =>
            {
                ChatMessagePart part = new ChatMessagePart();
                if (!string.IsNullOrEmpty(p.Text))
                {
                    part.Text = p.Text;
                }

                if (p.InlineData?.Data is not null)
                {
                    part.Type = ChatMessageTypes.Audio;
                    part.Audio = new ChatAudio(p.InlineData.Data, ChatAudioFormats.L16)
                    {
                        MimeType = p.InlineData.MimeType
                    };
                }

                return part;
            }).ToList()
        };

        return message;
    }

    private static LiveTranscription? MapTranscription(VendorGoogleLiveTranscription? transcription)
    {
        return transcription?.Text is null ? null : new LiveTranscription { Text = transcription.Text };
    }

    private static string? MapModality(LiveResponseModality modality) => modality switch
    {
        LiveResponseModality.Text => "TEXT",
        LiveResponseModality.Audio => "AUDIO",
        _ => null
    };

    private static string? MapThinkingLevel(LiveThinkingLevel? level) => level switch
    {
        LiveThinkingLevel.Minimal => "minimal",
        LiveThinkingLevel.Low => "low",
        LiveThinkingLevel.Medium => "medium",
        LiveThinkingLevel.High => "high",
        _ => null
    };

    private static string? MapMediaResolution(LiveMediaResolution? resolution) => resolution switch
    {
        LiveMediaResolution.Low => "MEDIA_RESOLUTION_LOW",
        LiveMediaResolution.Medium => "MEDIA_RESOLUTION_MEDIUM",
        LiveMediaResolution.High => "MEDIA_RESOLUTION_HIGH",
        _ => null
    };

    private static string? MapTurnCoverage(LiveTurnCoverage coverage) => coverage switch
    {
        LiveTurnCoverage.TurnIncludesOnlyActivity => "TURN_INCLUDES_ONLY_ACTIVITY",
        LiveTurnCoverage.TurnIncludesAllInput => "TURN_INCLUDES_ALL_INPUT",
        LiveTurnCoverage.TurnIncludesAudioActivityAndAllVideo => "TURN_INCLUDES_AUDIO_ACTIVITY_AND_ALL_VIDEO",
        _ => null
    };

    private static string? MapActivityHandling(LiveActivityHandling? handling) => handling switch
    {
        LiveActivityHandling.StartOfActivityInterrupts => "START_OF_ACTIVITY_INTERRUPTS",
        LiveActivityHandling.NoInterruption => "NO_INTERRUPTION",
        _ => null
    };

    private static string? MapStartSensitivity(LiveStartSensitivity? sensitivity) => sensitivity switch
    {
        LiveStartSensitivity.High => "START_SENSITIVITY_HIGH",
        LiveStartSensitivity.Low => "START_SENSITIVITY_LOW",
        _ => null
    };

    private static string? MapEndSensitivity(LiveEndSensitivity? sensitivity) => sensitivity switch
    {
        LiveEndSensitivity.High => "END_SENSITIVITY_HIGH",
        LiveEndSensitivity.Low => "END_SENSITIVITY_LOW",
        _ => null
    };
}
