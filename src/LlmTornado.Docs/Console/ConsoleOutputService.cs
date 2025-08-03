using Microsoft.JSInterop;

namespace LlmTornado.Docs.Console;

public class ConsoleOutputService : IAsyncDisposable
{
    private readonly IJSRuntime _jsRuntime;
    private IJSObjectReference? _jsModule;
        
    public event Action<ConsoleOutputViewModel>? OnConsoleOutputReceived;

    public event Action? OnConsoleCleared;

    private readonly List<ConsoleOutputViewModel> _logs = new();

    public IReadOnlyList<ConsoleOutputViewModel> Logs => _logs.AsReadOnly();
        
    private DotNetObjectReference<ConsoleOutputService>? _dotNetObjectReference;
        
    public ConsoleOutputService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
        _dotNetObjectReference = DotNetObjectReference.Create(this);
            
        InitializeAsync();
    }
        
    public async void InitializeAsync()
    {
        // Load JavaScript module
        _jsModule = await _jsRuntime.InvokeAsync<IJSObjectReference>("import", "./js/app.js");

        // Hook into JavaScript console
        await _jsModule.InvokeVoidAsync("captureConsoleOutput", _dotNetObjectReference);
    }
        
    [JSInvokable]
    public void OnConsoleLog(string message)
    {
        AddLog(message, ConsoleSeverity.Info); // Info severity for logs
    }

    [JSInvokable]
    public void OnConsoleError(string message)
    {
        AddLog(message, ConsoleSeverity.Error); // Error severity for errors
    }

    [JSInvokable]
    public void OnConsoleWarn(string message)
    {
        AddLog(message, ConsoleSeverity.Warning); // Warning severity for warnings
    }
        
    public void AddLog(string message, ConsoleSeverity severity)
    {
        var logEntry = new ConsoleOutputViewModel
        {
            Timestamp = DateTimeOffset.Now,
            Severity = severity,
            Message = message
        };

        _logs.Add(logEntry);
        OnConsoleOutputReceived?.Invoke(logEntry); 
    }

    public void ClearLogs()
    {
        _logs.Clear();
        OnConsoleCleared?.Invoke();
    }
        
    public async ValueTask DisposeAsync()
    {
        if (_jsModule is not null)
        {
            await _jsModule.DisposeAsync();
        }
    }
}