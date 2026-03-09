using LlmTornado.Chat;
using LlmTornado.Cli.Blazor.Models;
using LlmTornado.Code;

namespace LlmTornado.Cli.Blazor.Controllers;

public sealed partial class ChatRuntimeController
{
    // ─────────────────────────────────────────────
    // Conversation management
    // ─────────────────────────────────────────────

    public Task LoadConversationAsync(string conversationId)
    {
        if (_conversationStore is null || Ui is null) return Task.CompletedTask;

        Ui.SetLoading(true);
        Ui.Clear();

        List<ChatMessage>? messages = _conversationStore.Load(conversationId);
        if (messages is null)
        {
            Ui.SetLoading(false);
            return Task.CompletedTask;
        }

        _currentConversationId = conversationId;

        // Restore runtime state
        _runtime?.Clear();

        // Map and display each message (skipping system messages)
        foreach (ChatMessage msg in messages)
        {
            if (msg.Role == ChatMessageRoles.System) continue;
            Ui.AddMessage(MapToChatUiMessage(msg));
        }

        UpdateContextWindowStatus(_agentBuilder?.ActiveModel);

        Ui.SetLoading(false);
        return Task.CompletedTask;
    }

    public Task NewConversationAsync()
    {
        _currentConversationId = null;
        _runtime?.Clear();
        Ui?.Clear();
        ResetCurrentTurnTokenTelemetry();
        UpdateContextWindowStatus(_agentBuilder?.ActiveModel);
        RefreshConversationList();
        return Task.CompletedTask;
    }

    public Task DeleteConversationAsync(string conversationId)
    {
        _conversationStore?.Delete(conversationId);

        if (_currentConversationId == conversationId)
        {
            _currentConversationId = null;
            _runtime?.Clear();
            Ui?.Clear();
            ResetCurrentTurnTokenTelemetry();
            UpdateContextWindowStatus(_agentBuilder?.ActiveModel);
        }

        RefreshConversationList();
        return Task.CompletedTask;
    }

    private void RefreshConversationList()
    {
        if (_conversationStore is null || Ui is null) return;

        List<ChatUiConversation> convos = _conversationStore.List()
            .Select(meta => new ChatUiConversation
            {
                Id = meta.Id,
                Label = meta.Label,
                Preview = meta.FirstMessagePreview,
                UpdatedAt = meta.UpdatedAt,
                MessageCount = meta.MessageCount
            })
            .ToList();

        Ui.SetConversations(convos);
    }
}
