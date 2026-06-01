using LlmTornado.Chat.Models;
using LlmTornado.Code;
using LlmTornado.Common;
using LlmTornado.Demo;
using LlmTornado.Realtime;

namespace LlmTornado.Tests;

[TestFixture]
[Category("Integration")]
public class RealtimeTranscriptionTests
{
    [SetUp]
    public async Task Setup()
    {
        await Program.SetupApi();
    }

    [Test]
    public async Task CreateTranscriptionSession_ReturnsClientSecret()
    {
        TornadoApi api = Program.Connect();
        RealtimeTranscriptionSessionConfig config = RealtimeTranscriptionSessionConfig.ForRealtimeWhisper("en");

        HttpCallResult<RealtimeSessionCreateResponse> result =
            await api.Realtime.CreateTranscriptionSession(config);

        Assert.That(result.Ok, Is.True, result.Response);
        Assert.That(result.Data, Is.Not.Null);
        Assert.That(result.Data!.ClientSecret?.Value, Is.Not.Null.And.Not.Empty);
        Assert.That(result.Data.ClientSecret!.ExpiresAt, Is.GreaterThan(0));
    }

    [Test]
    public async Task CreateClientSecretForTranscription_ReturnsEphemeralKey()
    {
        TornadoApi api = Program.Connect();

        HttpCallResult<RealtimeClientSecretResponse> result =
            await api.Realtime.CreateClientSecretForTranscription(
                RealtimeTranscriptionSessionConfig.ForRealtimeWhisper("en"));

        Assert.That(result.Ok, Is.True, result.Response);
        Assert.That(result.Data?.Value, Is.Not.Null.And.Not.Empty);
        Assert.That(result.Data!.ExpiresAt, Is.GreaterThan(0));
    }

    [Test]
    public async Task TranscribeStreamingAsync_ProducesTranscript()
    {
        TornadoApi api = Program.Connect();
        string pcmPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "Static", "Audio", "sample.pcm");

        Assert.That(File.Exists(pcmPath), Is.True, $"Missing test audio: {pcmPath}");

        byte[] pcm = await File.ReadAllBytesAsync(pcmPath);
        byte[] clip = pcm.Length > 24000 * 2 * 3 ? pcm[..(24000 * 2 * 3)] : pcm;

        RealtimeTranscriptionResult result = await api.Realtime.TranscribeStreamingAsync(
            clip,
            RealtimeTranscriptionSessionConfig.ForRealtimeWhisper("en", "low"));

        TestContext.WriteLine($"Final: {result.FinalTranscript}");
        TestContext.WriteLine($"Partial: {result.PartialTranscript}");
        TestContext.WriteLine($"Deltas: {result.Deltas.Count}");
        if (result.Errors.Count > 0)
        {
            TestContext.WriteLine($"Errors: {string.Join("; ", result.Errors)}");
        }

        string? text = result.FinalTranscript ?? result.PartialTranscript;
        Assert.That(text, Is.Not.Null.And.Not.Empty, "Expected non-empty transcript from streaming STT");
    }

    [Test]
    public void GptRealtimeWhisper_Model_IsRegistered()
    {
        ChatModel model = ChatModel.OpenAi.Realtime.RealtimeWhisper;
        Assert.That(model.Name, Is.EqualTo("gpt-realtime-whisper"));
        Assert.That(model.Provider, Is.EqualTo(LLmProviders.OpenAi));
    }
}
