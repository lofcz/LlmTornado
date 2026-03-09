using LlmTornado.Chat;
using LlmTornado.Cli.Blazor.Models;
using LlmTornado.Code;

namespace LlmTornado.Cli.Blazor.Controllers;

public sealed partial class ChatRuntimeController
{
    // ─────────────────────────────────────────────
    // Chat actions
    // ─────────────────────────────────────────────

    public async Task SendMessageAsync(string text, List<ChatUiFile>? files)
    {
        if (_runtime is null || Ui is null) return;

        ResetCurrentTurnTokenTelemetry();

        // 1. Display user message
        var userMsg = new ChatUiMessage
        {
            Role = ChatUiRole.User,
            Content = text,
            Files = files ?? []
        };
        _currentUserMessageId = userMsg.Id;
        Ui.AddMessage(userMsg);

        // 2. Build internal ChatMessage (with file attachments if any)
        ChatMessage chatMessage = BuildChatMessage(text, files);

        // 3. Start streaming response
        string streamingId = Guid.NewGuid().ToString();
        _currentStreamingId = streamingId;
        Ui.StartStreamingMessage(streamingId);

        try
        {
            // 4. Tool optimization if needed
            if (_agentBuilder!.NeedsOptimization)
            {
                await _agentBuilder.OptimizeToolsForTurn(text);
            }

            // 5. Invoke the runtime
            ChatMessage response = await _runtime.InvokeAsync(chatMessage);

            // 6. Restore full tools after optimization
            if (_agentBuilder.NeedsOptimization)
            {
                _agentBuilder.RestoreFullTools();
            }

            // 7. Finalize streaming
            Ui.CompleteStreamingMessage(streamingId);

            // 8. Ensure conversation ID exists
            _currentConversationId ??= Guid.NewGuid().ToString();

            // 9. Save/update conversation
            List<ChatMessage> allMessages = _runtime.RuntimeConfiguration.GetMessages();
            string? modelName = _agentBuilder.ActiveModel.Name;
            List<string> activeSkills = _skillManager?.GetEnabledSkills()
                .Select(s => s.Name).ToList() ?? [];

            _currentConversationId = _conversationStore!.Save(allMessages, modelName, activeSkills, null, _currentConversationId);

            RefreshConversationList();
        }
        catch (Exception ex)
        {
            Ui.CompleteStreamingMessage(streamingId);
            Ui.AddMessage(new ChatUiMessage
            {
                Role = ChatUiRole.Assistant,
                Content = $"**Error:** {ex.Message}",
                IsError = true
            });
        }
        finally
        {
            _currentUserMessageId = null;
            _currentStreamingId = null;
        }
    }

    public Task CancelAsync()
    {
        _runtime?.CancelExecution();

        if (_currentStreamingId is not null && Ui is not null)
        {
            Ui.CompleteStreamingMessage(_currentStreamingId);
            _currentStreamingId = null;
        }

        // Cancel any pending tool approvals
        foreach (var kvp in _pendingApprovals)
        {
            kvp.Value.Completion.TrySetResult(false);
        }
        _pendingApprovals.Clear();

        return Task.CompletedTask;
    }

    // ─────────────────────────────────────────────
    // Message building
    // ─────────────────────────────────────────────

    private ChatMessage BuildChatMessage(string text, List<ChatUiFile>? files)
    {
        if (files is null || files.Count == 0)
        {
            return new ChatMessage(ChatMessageRoles.User, text);
        }

        // Build multipart message with file attachments
        List<ChatMessagePart> parts = [];

        foreach (ChatUiFile file in files)
        {
            string base64 = file.Base64;

            if (file.IsImage)
            {
                string dataUri = $"data:{file.MimeType};base64,{base64}";
                parts.Add(new ChatMessagePart(dataUri, LlmTornado.Images.ImageDetail.Auto, file.MimeType));
            }
            else if (file.IsDocument)
            {
                parts.Add(new ChatMessagePart(new ChatDocument(base64)));
            }
            else if (file.IsAudio)
            {
                ChatAudioFormats format = file.MimeType switch
                {
                    "audio/wav" => ChatAudioFormats.Wav,
                    "audio/mpeg" or "audio/mp3" => ChatAudioFormats.Mp3,
                    _ => ChatAudioFormats.Wav,
                };
                parts.Add(new ChatMessagePart(file.Content, format));
            }
        }

        if (!string.IsNullOrWhiteSpace(text))
        {
            parts.Add(new ChatMessagePart(text));
        }

        return new ChatMessage(ChatMessageRoles.User, parts);
    }
}
