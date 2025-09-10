using System;
using System.Collections.Generic;

namespace LlmTornado.Docs.Code.Intellisense;

public enum IntelliStage
{
    Starting = 0,
    LoadingAssemblies = 10,
    SpinningWorker = 35,
    WiringChannels = 60,
    WarmupProject = 85,
    Ready = 100
}

public record IntelliStatus(
    IntelliStage Stage,
    int Percent,
    string Message,
    DateTime TimestampUtc
);

public interface IIntellisenseStatus
{
    bool IsReady { get; }
    IReadOnlyList<IntelliStatus> History { get; }
    event Action<IntelliStatus>? Updated;

    void ResetIfNotReady();
    void Publish(IntelliStage stage, int percent, string message);
    void MarkReady();
}

public class IntellisenseStatus : IIntellisenseStatus
{
    private readonly List<IntelliStatus> _history = new();
    private bool _ready;

    public bool IsReady => _ready;
    public IReadOnlyList<IntelliStatus> History => _history;

    public event Action<IntelliStatus>? Updated;

    public void ResetIfNotReady()
    {
        if (_ready) return;
        _history.Clear();
    }

    public void Publish(IntelliStage stage, int percent, string message)
    {
        var status = new IntelliStatus(stage, percent, message, DateTime.UtcNow);
        _history.Add(status);
        Updated?.Invoke(status);
    }

    public void MarkReady()
    {
        _ready = true;
        var status = new IntelliStatus(IntelliStage.Ready, 100, "Ready", DateTime.UtcNow);
        _history.Add(status);
        Updated?.Invoke(status);
    }
}