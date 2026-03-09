namespace LlmTornado.Cli.Blazor.Models;

public sealed class ChatUiContextWindowStatus
{
    public string ModelName { get; set; } = string.Empty;
    public int MaxTokens { get; set; }
    public int? UsedTokens { get; set; }
    public int? RemainingTokens { get; set; }
    public double? Utilization { get; set; }
    public string? CountingMethod { get; set; }

    public bool HasTelemetry => UsedTokens is not null;
}