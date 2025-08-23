using LlmTornado.Agents.DataModels;
using LlmTornado.Chat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LlmTornado.Agents.ChatRuntime;

public interface IRuntimeConfiguration
{
    public ValueTask<ChatMessage> AddToChatAsync(ChatMessage message, CancellationToken cancellationToken = default);

    public CancellationTokenSource cts { get; set; }

    public Func<ModelStreamingEvents, ValueTask>? OnRuntimeEvent { get; }

    public List<ChatMessage> GetMessages();

    public ChatMessage GetLastMessage();

    public void ClearMessages();
}
