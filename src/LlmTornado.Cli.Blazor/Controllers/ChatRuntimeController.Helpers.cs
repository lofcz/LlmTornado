using LlmTornado.Chat;
using LlmTornado.Cli.Blazor.Models;
using LlmTornado.Cli.Core;
using LlmTornado.Cli.Core.Agents;
using LlmTornado.Code;

namespace LlmTornado.Cli.Blazor.Controllers;

public sealed partial class ChatRuntimeController
{
    // ─────────────────────────────────────────────
    // ISettingsPersistence implementation
    // ─────────────────────────────────────────────

    void ISettingsPersistence.SaveSettings(AgentSettings settings)
    {
        SaveSettings(settings);
    }

    // ─────────────────────────────────────────────
    // Mapping helpers
    // ─────────────────────────────────────────────

    private ChatUiMessage MapToChatUiMessage(ChatMessage msg)
    {
        ChatUiRole role = msg.Role switch
        {
            ChatMessageRoles.User => ChatUiRole.User,
            ChatMessageRoles.System => ChatUiRole.System,
            _ => ChatUiRole.Assistant
        };

        var uiMsg = new ChatUiMessage
        {
            Role = role,
            Content = msg.Content ?? string.Empty,
            Timestamp = DateTime.UtcNow
        };

        if (msg.Parts is not null && msg.Parts.Count > 0)
        {
            foreach (ChatMessagePart part in msg.Parts)
            {
                if (!string.IsNullOrEmpty(part.Text))
                {
                    if (string.IsNullOrEmpty(uiMsg.Content))
                    {
                        uiMsg.Content = part.Text;
                    }
                }
                else if (part.Type == ChatMessageTypes.Image && part.Image is not null && !string.IsNullOrEmpty(part.Image.Url))
                {
                    string url = part.Image.Url;
                    string mimeType = part.Image.MimeType ?? "image/jpeg";
                    byte[] content = [];

                    if (url.StartsWith("data:"))
                    {
                        int commaIndex = url.IndexOf(',');
                        if (commaIndex >= 0)
                        {
                            string header = url[..commaIndex];
                            int semiIndex = header.IndexOf(';');
                            if (semiIndex >= 0)
                            {
                                mimeType = header[5..semiIndex];
                            }
                            string base64Str = url[(commaIndex + 1)..];
                            try { content = Convert.FromBase64String(base64Str); } catch { }
                        }
                    }
                    else
                    {
                        try { content = Convert.FromBase64String(url); } catch { }
                    }

                    if (content.Length > 0)
                    {
                        uiMsg.Files.Add(new ChatUiFile
                        {
                            FileName = $"image.{mimeType.Split('/').LastOrDefault() ?? "jpeg"}",
                            MimeType = mimeType,
                            Content = content
                        });
                    }
                }
                else if (part.Type == ChatMessageTypes.Document && part.Document is not null)
                {
                    string data = part.Document.Base64 ?? string.Empty;
                    byte[] content = [];
                    try { content = Convert.FromBase64String(data); } catch { }
                    if (content.Length > 0)
                    {
                        uiMsg.Files.Add(new ChatUiFile
                        {
                            FileName = "document.pdf",
                            MimeType = "application/pdf",
                            Content = content
                        });
                    }
                }
                else if (part.Type == ChatMessageTypes.Audio && part.Audio is not null)
                {
                    string data = part.Audio.Data ?? string.Empty;
                    byte[] content = [];
                    try { content = Convert.FromBase64String(data); } catch { }
                    if (content.Length > 0)
                    {
                        string mimeType = part.Audio.MimeType ?? "audio/wav";
                        uiMsg.Files.Add(new ChatUiFile
                        {
                            FileName = $"audio.{mimeType.Split('/').LastOrDefault() ?? "wav"}",
                            MimeType = mimeType,
                            Content = content
                        });
                    }
                }
            }
        }

        return uiMsg;
    }

    private static ChatUiAgent MapAgent(AgentDefinition def) => new()
    {
        Name = def.Name,
        Description = def.Description,
        Source = def.Source switch
        {
            AgentSource.BuiltIn => ChatUiAgentSource.BuiltIn,
            AgentSource.Global => ChatUiAgentSource.Global,
            AgentSource.Custom => ChatUiAgentSource.Custom,
            AgentSource.Project => ChatUiAgentSource.Project,
            _ => ChatUiAgentSource.Custom
        },
        HasCapabilityCuration = def.HasCapabilityCuration
    };

    // ─────────────────────────────────────────────
    // Settings persistence
    // ─────────────────────────────────────────────

    private void ApplyApiKeyOverrides()
    {
        if (_options.ApiKeyOverrides is null) return;

        foreach (var (envVar, apiKey) in _options.ApiKeyOverrides)
        {
            if (!string.IsNullOrWhiteSpace(apiKey))
            {
                Environment.SetEnvironmentVariable(envVar, apiKey);
            }
        }
    }

    private AgentSettings LoadSettings()
    {
        if (File.Exists(_settingsPath))
        {
            try
            {
                string json = File.ReadAllText(_settingsPath);
                return System.Text.Json.JsonSerializer.Deserialize<AgentSettings>(json) ?? new();
            }
            catch
            {
                return new();
            }
        }
        return new();
    }

    private void SaveSettings(AgentSettings settings)
    {
        try
        {
            string? dir = Path.GetDirectoryName(_settingsPath);
            if (dir is not null) Directory.CreateDirectory(dir);

            string json = System.Text.Json.JsonSerializer.Serialize(settings,
                new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_settingsPath, json);
        }
        catch
        {
            // Settings persistence failure is non-fatal
        }
    }

    // ─────────────────────────────────────────────
    // Utilities
    // ─────────────────────────────────────────────

    private static string ExtractToolName(string requestMessage)
    {
        // Request messages follow the pattern "Tool 'name' wants to..."
        int start = requestMessage.IndexOf('\'');
        int end = requestMessage.IndexOf('\'', start + 1);
        if (start >= 0 && end > start)
            return requestMessage[(start + 1)..end];
        return "unknown-tool";
    }
}
