using LlmTornado.Agents.DataModels;
using LlmTornado.Chat;
using LlmTornado.Tokenize;

namespace LlmTornado.Agents.Utility;

internal static class AgentTokenTelemetryCalculator
{
    public static async Task<AgentRequestTokenTelemetry> CalculatePreflightAsync(
        TornadoAgent agent,
        Conversation requestConversation,
        List<ChatMessage>? messagesBeforeInput,
        string? responseId,
        CancellationToken cancellationToken = default)
    {
        TokenCountMeasurement requestMeasurement = await CountConversationTokensAsync(
            agent.Client,
            requestConversation,
            cancellationToken).ConfigureAwait(false);

        int? contextTokensBeforeInput = null;

        if (messagesBeforeInput is not null)
        {
            Conversation memoryConversation = CreateConversation(agent, messagesBeforeInput, responseId, cancellationToken);
            TokenCountMeasurement memoryMeasurement = await CountConversationTokensAsync(
                agent.Client,
                memoryConversation,
                cancellationToken).ConfigureAwait(false);

            contextTokensBeforeInput = memoryMeasurement.TotalTokens;
            if (memoryMeasurement.Source == AgentTokenMeasurementSource.EstimatorFallback)
            {
                requestMeasurement = requestMeasurement with
                {
                    Source = AgentTokenMeasurementSource.EstimatorFallback
                };
            }
        }

        int contextWindowTokens = TokenEstimator.GetContextWindowSize(agent.Model);

        return new AgentRequestTokenTelemetry
        {
            ContextTokensBeforeInput = contextTokensBeforeInput,
            RequestTokensBeforeSend = requestMeasurement.TotalTokens,
            ContextWindowTokens = contextWindowTokens,
            ContextWindowUtilization = TokenEstimator.CalculateUtilization(requestMeasurement.TotalTokens, contextWindowTokens),
            Source = requestMeasurement.Source,
            ModelName = agent.Model.Name
        };
    }

    private static async Task<TokenCountMeasurement> CountConversationTokensAsync(
        TornadoApi api,
        Conversation conversation,
        CancellationToken cancellationToken)
    {
        ChatRequest request = new ChatRequest(conversation.RequestParameters)
        {
            Messages = conversation.Messages.ToList()
        };

        try
        {
            TokenizeResult? result = await api.Tokenize.CountTokens(
                new TokenizeRequest(request),
                cancellationToken).ConfigureAwait(false);

            if (result is not null && result.TotalTokens > 0)
            {
                return new TokenCountMeasurement(result.TotalTokens, AgentTokenMeasurementSource.ProviderTokenizer);
            }
        }
        catch
        {
            // Fall back to estimator for providers without tokenize support or tokenization failures.
        }

        return new TokenCountMeasurement(
            TokenEstimator.EstimateTotalTokens(conversation.Messages.ToList()),
            AgentTokenMeasurementSource.EstimatorFallback);
    }

    private static Conversation CreateConversation(
        TornadoAgent agent,
        List<ChatMessage>? messages,
        string? responseId,
        CancellationToken cancellationToken)
    {
        Conversation conversation = agent.Client.Chat.CreateConversation(agent.Options);
        conversation.RequestParameters.CancellationToken = cancellationToken;

        if (messages is not null)
        {
            int lastIndex = messages.Count - 1;
            foreach (ChatMessage message in messages)
            {
                if (messages.Count > 0 && ReferenceEquals(message, messages[lastIndex]) && message.Role == Code.ChatMessageRoles.User)
                {
                    continue;
                }

                if (message.Role == Code.ChatMessageRoles.System)
                {
                    continue;
                }

                conversation.AppendMessage(message);
            }
        }

        if (!string.IsNullOrEmpty(responseId) && conversation.RequestParameters.ResponseRequestParameters is not null)
        {
            conversation.RequestParameters.ResponseRequestParameters.PreviousResponseId = responseId;
        }

        conversation.AddSystemMessage(agent.Instructions);
        return conversation;
    }

    private readonly record struct TokenCountMeasurement(int TotalTokens, AgentTokenMeasurementSource Source);
}