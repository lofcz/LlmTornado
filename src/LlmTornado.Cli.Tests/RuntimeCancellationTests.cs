using System.Net;
using LlmTornado.Agents;
using LlmTornado.Agents.ChatRuntime;
using LlmTornado.Agents.ChatRuntime.RuntimeConfigurations;
using LlmTornado.Agents.DataModels;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Code;

namespace LlmTornado.Cli.Tests;

/// <summary>
/// Guards the cancellation wiring in <see cref="SingletonRuntimeConfiguration"/>: the runtime's
/// CTS must actually reach the in-flight request (linked with the caller token), and a spent CTS
/// must be re-armed so one Ctrl+C doesn't pre-cancel every later turn.
/// </summary>
[TestFixture]
public class RuntimeCancellationTests
{
    private static (SingletonRuntimeConfiguration Config, List<AgentRunnerEvents> Events) CreateRuntime(string baseUrl)
    {
        TornadoApi api = new(new Uri(baseUrl), "unused-key");
        TornadoAgent agent = new(api, new ChatModel("test-model", LLmProviders.Custom), "Agent", "test", streaming: false);
        SingletonRuntimeConfiguration config = new(agent);

        List<AgentRunnerEvents> events = [];
        config.OnRuntimeEvent = evt =>
        {
            if (evt is ChatRuntimeAgentRunnerEvents runnerEvt)
            {
                lock (events)
                    events.Add(runnerEvt.AgentRunnerEvent);
            }
            return ValueTask.CompletedTask;
        };

        _ = new ChatRuntime(config); // binds config.Runtime (used for event ids)
        return (config, events);
    }

    [Test]
    public async Task CancelledRuntimeCts_IsRearmed_OnNextTurn()
    {
        (SingletonRuntimeConfiguration config, _) = CreateRuntime("http://127.0.0.1:9");

        config.CancelRuntime();
        Assert.That(config.cts.IsCancellationRequested, Is.True);

        // The next turn must not inherit the spent CTS. Pass an already-cancelled caller token so
        // the run exits at the first cancellation check instead of hitting the (dead) endpoint.
        using CancellationTokenSource callerCts = new();
        callerCts.Cancel();
        await config.AddToChatAsync(new ChatMessage(ChatMessageRoles.User, "hi"), callerCts.Token);

        Assert.That(config.cts.IsCancellationRequested, Is.False, "spent CTS was not re-armed");
    }

    [Test]
    public async Task CancelRuntime_MidRequest_AbortsTheTurnPromptly()
    {
        // A local endpoint that accepts the request and then stalls far longer than the test
        // is willing to wait — only a real mid-flight abort can finish the turn quickly.
        using HttpListener listener = new();
        int port = FindFreePort();
        listener.Prefixes.Add($"http://127.0.0.1:{port}/");
        listener.Start();

        CancellationTokenSource serverStop = new();
        Task server = Task.Run(async () =>
        {
            try
            {
                while (listener.IsListening)
                {
                    HttpListenerContext ctx = await listener.GetContextAsync();
                    await Task.Delay(TimeSpan.FromSeconds(30), serverStop.Token).ContinueWith(_ => { });
                    try { ctx.Response.StatusCode = 500; ctx.Response.Close(); } catch { }
                }
            }
            catch { /* listener stopped */ }
        });

        try
        {
            (SingletonRuntimeConfiguration config, List<AgentRunnerEvents> events) =
                CreateRuntime($"http://127.0.0.1:{port}");

            Task turn = config.AddToChatAsync(new ChatMessage(ChatMessageRoles.User, "hi")).AsTask();

            // Let the request reach the stalled server, then interrupt.
            await Task.Delay(500);
            config.CancelRuntime();

            Task finished = await Task.WhenAny(turn, Task.Delay(TimeSpan.FromSeconds(10)));
            Assert.That(finished, Is.SameAs(turn), "CancelRuntime did not abort the in-flight request");
            await turn; // must complete gracefully, not throw

            bool sawCancellation;
            lock (events)
            {
                sawCancellation = events.Any(e =>
                    e is AgentRunnerCancelledEvent ||
                    e is AgentRunnerErrorEvent { Exception: OperationCanceledException });
            }
            Assert.That(sawCancellation, Is.True, "no cancellation signal surfaced through runner events");
        }
        finally
        {
            serverStop.Cancel();
            listener.Stop();
            await server;
        }
    }

    private static int FindFreePort()
    {
        System.Net.Sockets.TcpListener probe = new(IPAddress.Loopback, 0);
        probe.Start();
        int port = ((IPEndPoint)probe.LocalEndpoint).Port;
        probe.Stop();
        return port;
    }
}
