using LlmTornado.Chat.Models;
using LlmTornado.Code;
using LlmTornado.Common;
using LlmTornado.Demo;
using LlmTornado.Realtime;

namespace LlmTornado.Tests;

/// <summary>
/// Integration tests for OpenAI Realtime 2 / GA Realtime API.
/// </summary>
[TestFixture]
[Category("Integration")]
public class RealtimeTests
{
    private TornadoApi? api;

    [SetUp]
    public async Task Setup()
    {
        await Program.SetupApi();
        string? key = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        if (string.IsNullOrWhiteSpace(key))
        {
            api = null;
            return;
        }

        api = new TornadoApi(LLmProviders.OpenAi, key);
    }

    [Test]
    public void RealtimeModels_AreRegistered()
    {
        Assert.That(ChatModel.OpenAi.Realtime.Realtime2.Name, Is.EqualTo("gpt-realtime-2"));
        Assert.That(ChatModel.OpenAi.Realtime.RealtimeTranslate.Name, Is.EqualTo("gpt-realtime-translate"));
        Assert.That(ChatModel.OpenAi.Realtime.RealtimeWhisper.Name, Is.EqualTo("gpt-realtime-whisper"));
        Assert.That(ChatModel.OpenAi.OwnsModel("gpt-realtime-2"), Is.True);
    }

    [Test]
    public void RealtimeConnectOptions_BuildsGaWebSocketUrls()
    {
        RealtimeConnectOptions voice = new RealtimeConnectOptions
        {
            Kind = RealtimeSessionKind.Voice,
            Model = ChatModel.OpenAi.Realtime.Realtime2
        };
        Assert.That(voice.BuildWebSocketUri().ToString(), Is.EqualTo("wss://api.openai.com/v1/realtime?model=gpt-realtime-2"));

        RealtimeConnectOptions translation = new RealtimeConnectOptions { Kind = RealtimeSessionKind.Translation };
        Assert.That(translation.BuildWebSocketUri().ToString(), Is.EqualTo("wss://api.openai.com/v1/realtime/translations?model=gpt-realtime-translate"));
    }

    [Test]
    public async Task CreateClientSecret_ForRealtime2_ReturnsEphemeralKey()
    {
        if (api is null)
        {
            Assert.Ignore("OPENAI_API_KEY environment variable not set.");
            return;
        }

        HttpCallResult<RealtimeClientSecretResponse> result = await api.Realtime.CreateClientSecretForRealtime2(
            RealtimeVoiceSessionConfig.ForRealtime2("You are a concise voice assistant.", RealtimeReasoningEffort.Low));

        Assert.That(result.Ok, Is.True, result.Response);
        Assert.That(result.Data?.Value, Does.StartWith("ek_").Or.Not.Empty);
        Assert.That(result.Data?.Session?["type"]?.ToString(), Is.EqualTo("realtime").Or.Null);
    }

    [Test]
    public async Task CreateClientSecret_ForTranscription_ReturnsEphemeralKey()
    {
        if (api is null)
        {
            Assert.Ignore("OPENAI_API_KEY environment variable not set.");
            return;
        }

        HttpCallResult<RealtimeClientSecretResponse> result = await api.Realtime.CreateClientSecretForTranscription();

        Assert.That(result.Ok, Is.True, result.Response);
        Assert.That(result.Data?.Value, Is.Not.Empty);
    }

    [Test]
    public async Task WebSocket_Connect_ReceivesSessionCreated()
    {
        if (api is null)
        {
            Assert.Ignore("OPENAI_API_KEY environment variable not set.");
            return;
        }

        TaskCompletionSource<bool> sessionCreated = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        await using RealtimeSession session = await api.Realtime.ConnectAsync(new RealtimeConnectOptions
        {
            Kind = RealtimeSessionKind.Voice,
            Model = ChatModel.OpenAi.Realtime.Realtime2,
            OnEvent = evt =>
            {
                if (evt.Type is RealtimeEventTypes.SessionCreated or RealtimeEventTypes.SessionUpdated)
                {
                    sessionCreated.TrySetResult(true);
                }
            }
        });

        await session.UpdateSessionAsync(RealtimeVoiceSessionConfig.ForRealtime2("Say hello briefly.", RealtimeReasoningEffort.Low));

        bool gotEvent = await Task.WhenAny(sessionCreated.Task, Task.Delay(TimeSpan.FromSeconds(15))) == sessionCreated.Task
                        && await sessionCreated.Task;

        Assert.That(gotEvent, Is.True, "Expected session.created or session.updated within 15s");
        Assert.That(session.IsOpen, Is.True);
    }
}
