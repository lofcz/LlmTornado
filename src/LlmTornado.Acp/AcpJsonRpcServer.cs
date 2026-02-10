using System.Text;
using System.Text.Json;

namespace LlmTornado.Acp;

/// <summary>
/// JSON-RPC 2.0 server for the Agent Client Protocol, communicating over stdio.
/// Handles incoming client requests and dispatches them to the ACP runtime configuration.
/// </summary>
public class AcpJsonRpcServer
{
    private readonly IAcpRuntimeConfiguration _runtime;
    private readonly Stream _inputStream;
    private readonly Stream _outputStream;
    private readonly CancellationTokenSource _serverCts = new();
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// Creates a new ACP JSON-RPC server using stdin/stdout.
    /// </summary>
    public AcpJsonRpcServer(IAcpRuntimeConfiguration runtime)
        : this(runtime, Console.OpenStandardInput(), Console.OpenStandardOutput())
    {
    }

    /// <summary>
    /// Creates a new ACP JSON-RPC server using custom streams.
    /// </summary>
    public AcpJsonRpcServer(IAcpRuntimeConfiguration runtime, Stream inputStream, Stream outputStream)
    {
        _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
        _inputStream = inputStream ?? throw new ArgumentNullException(nameof(inputStream));
        _outputStream = outputStream ?? throw new ArgumentNullException(nameof(outputStream));

        _runtime.OnSessionUpdate += HandleSessionUpdate;
    }

    /// <summary>
    /// Starts the JSON-RPC server and processes messages until cancellation.
    /// </summary>
    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _serverCts.Token);
        using StreamReader reader = new(_inputStream, Encoding.UTF8, leaveOpen: true);

        while (!linkedCts.Token.IsCancellationRequested)
        {
            string? line = await reader.ReadLineAsync(linkedCts.Token);

            if (line is null)
            {
                break;
            }

            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            await ProcessMessageAsync(line, linkedCts.Token);
        }
    }

    /// <summary>
    /// Stops the JSON-RPC server.
    /// </summary>
    public void Stop()
    {
        _serverCts.Cancel();
    }

    private async Task ProcessMessageAsync(string message, CancellationToken cancellationToken)
    {
        try
        {
            using JsonDocument doc = JsonDocument.Parse(message);
            JsonElement root = doc.RootElement;

            string method = root.TryGetProperty("method", out JsonElement methodElement)
                ? methodElement.GetString() ?? string.Empty
                : string.Empty;

            object? id = root.TryGetProperty("id", out JsonElement idElement)
                ? DeserializeId(idElement)
                : null;

            JsonElement paramsElement = root.TryGetProperty("params", out JsonElement p) ? p : default;

            // Notifications (no id)
            if (id is null)
            {
                await HandleNotificationAsync(method, paramsElement, cancellationToken);
                return;
            }

            // Requests (have id)
            object? result = await HandleRequestAsync(method, paramsElement, cancellationToken);

            await SendResponseAsync(id, result);
        }
        catch (JsonException)
        {
            await SendErrorAsync(null, AcpErrorCodes.ParseError, "Invalid JSON.");
        }
        catch (Exception ex)
        {
            await SendErrorAsync(null, AcpErrorCodes.InternalError, ex.Message);
        }
    }

    private async Task<object?> HandleRequestAsync(string method, JsonElement paramsElement, CancellationToken cancellationToken)
    {
        try
        {
            return method switch
            {
                AcpMethods.Initialize => await HandleInitializeAsync(paramsElement, cancellationToken),
                AcpMethods.Authenticate => await HandleAuthenticateAsync(paramsElement, cancellationToken),
                AcpMethods.NewSession => await HandleNewSessionAsync(paramsElement, cancellationToken),
                AcpMethods.Prompt => await HandlePromptAsync(paramsElement, cancellationToken),
                _ => throw new NotSupportedException($"Method '{method}' is not supported.")
            };
        }
        catch (NotSupportedException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error processing '{method}': {ex.Message}", ex);
        }
    }

    private async Task HandleNotificationAsync(string method, JsonElement paramsElement, CancellationToken cancellationToken)
    {
        switch (method)
        {
            case AcpMethods.Cancel:
                AcpCancelNotification? cancelNotification = paramsElement.ValueKind != JsonValueKind.Undefined
                    ? JsonSerializer.Deserialize<AcpCancelNotification>(paramsElement.GetRawText(), JsonOptions)
                    : null;

                if (cancelNotification is not null)
                {
                    await _runtime.CancelAsync(cancelNotification, cancellationToken);
                }

                break;
        }
    }

    private async Task<AcpInitializeResponse> HandleInitializeAsync(JsonElement paramsElement, CancellationToken cancellationToken)
    {
        AcpInitializeRequest request = paramsElement.ValueKind != JsonValueKind.Undefined
            ? JsonSerializer.Deserialize<AcpInitializeRequest>(paramsElement.GetRawText(), JsonOptions)!
            : new AcpInitializeRequest();

        return await _runtime.InitializeAsync(request, cancellationToken);
    }

    private Task<AcpAuthenticateResponse> HandleAuthenticateAsync(JsonElement paramsElement, CancellationToken cancellationToken)
    {
        return Task.FromResult(new AcpAuthenticateResponse());
    }

    private async Task<AcpNewSessionResponse> HandleNewSessionAsync(JsonElement paramsElement, CancellationToken cancellationToken)
    {
        AcpNewSessionRequest request = paramsElement.ValueKind != JsonValueKind.Undefined
            ? JsonSerializer.Deserialize<AcpNewSessionRequest>(paramsElement.GetRawText(), JsonOptions)!
            : new AcpNewSessionRequest();

        return await _runtime.NewSessionAsync(request, cancellationToken);
    }

    private async Task<AcpPromptResponse> HandlePromptAsync(JsonElement paramsElement, CancellationToken cancellationToken)
    {
        AcpPromptRequest request = JsonSerializer.Deserialize<AcpPromptRequest>(paramsElement.GetRawText(), JsonOptions)!;
        return await _runtime.PromptAsync(request, cancellationToken);
    }

    private async Task HandleSessionUpdate(AcpSessionNotification notification)
    {
        AcpJsonRpcNotification jsonRpcNotification = new()
        {
            Method = AcpMethods.Update,
            Params = notification
        };

        string json = JsonSerializer.Serialize(jsonRpcNotification, JsonOptions);
        byte[] bytes = Encoding.UTF8.GetBytes(json + "\n");
        await _outputStream.WriteAsync(bytes);
        await _outputStream.FlushAsync();
    }

    private async Task SendResponseAsync(object? id, object? result)
    {
        AcpJsonRpcResponse response = new()
        {
            Id = id,
            Result = result
        };

        string json = JsonSerializer.Serialize(response, JsonOptions);
        byte[] bytes = Encoding.UTF8.GetBytes(json + "\n");
        await _outputStream.WriteAsync(bytes);
        await _outputStream.FlushAsync();
    }

    private async Task SendErrorAsync(object? id, int code, string message)
    {
        AcpJsonRpcErrorResponse response = new()
        {
            Id = id,
            Error = new AcpError
            {
                Code = code,
                Message = message
            }
        };

        string json = JsonSerializer.Serialize(response, JsonOptions);
        byte[] bytes = Encoding.UTF8.GetBytes(json + "\n");
        await _outputStream.WriteAsync(bytes);
        await _outputStream.FlushAsync();
    }

    private static object? DeserializeId(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Number => element.GetInt64(),
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Null => null,
            _ => element.GetRawText()
        };
    }
}
