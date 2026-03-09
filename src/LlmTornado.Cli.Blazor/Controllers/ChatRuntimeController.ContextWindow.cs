using LlmTornado.Agents;
using LlmTornado.Chat.Models;
using LlmTornado.Cli.Blazor.Models;

namespace LlmTornado.Cli.Blazor.Controllers;

public sealed partial class ChatRuntimeController
{
    private void UpdateContextWindowStatus(ChatModel? model, ChatUiTokenTelemetry? telemetry = null)
    {
        if (Ui is null)
        {
            return;
        }

        int maxTokens = TokenEstimator.GetContextWindowSize(model);
        int? usedTokens = telemetry?.RequestTokensBeforeSend;

        Ui.SetContextWindowStatus(new ChatUiContextWindowStatus
        {
            ModelName = model?.Name ?? "Unknown model",
            MaxTokens = maxTokens,
            UsedTokens = usedTokens,
            RemainingTokens = usedTokens is null ? null : Math.Max(0, maxTokens - usedTokens.Value),
            Utilization = telemetry?.ContextWindowUtilization,
            CountingMethod = telemetry?.CountingMethod
        });
    }
}