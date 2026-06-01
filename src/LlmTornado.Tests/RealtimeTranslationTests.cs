using System.Text;
using LlmTornado.Audio;
using LlmTornado.Chat.Models;
using LlmTornado.Demo;
using LlmTornado.Realtime;
using LlmTornado.Realtime.Translation;
using LlmTornado.Realtime.Vendors.OpenAi;

namespace LlmTornado.Tests;

[TestFixture]
[Category("Integration")]
public class RealtimeTranslationTests
{
    [SetUp]
    public async Task Setup()
    {
        await Program.SetupApi();
    }

    [Test]
    [Timeout(120_000)]
    public async Task StreamTranslation_EnglishToSpanish_ReceivesAudioAndTranscripts()
    {
        TornadoApi api = Program.Connect();
        byte[] pcm16 = await GenerateEnglishPcm16Async(api);

        StringBuilder inputTranscript = new StringBuilder();
        StringBuilder outputTranscript = new StringBuilder();
        List<byte[]> outputAudioChunks = [];
        bool sessionCreated = false;
        bool sessionClosed = false;

        await using RealtimeTranslationSession session = await api.Realtime.Translation.ConnectAsync(
            new RealtimeTranslationConnectOptions
            {
                Model = ChatModelOpenAiRealtime.ModelRealtimeTranslate,
                Config = new RealtimeTranslationSessionConfig
                {
                    OutputLanguage = "es",
                    InputTranscriptionModel = ChatModelOpenAiRealtime.ModelRealtimeWhisper,
                    NoiseReduction = RealtimeTranslationNoiseReduction.NearField
                },
                EventHandler = new RealtimeTranslationEventHandler
                {
                    SessionHandler = evt =>
                    {
                        if (evt.EventType is RealtimeTranslationEventTypes.SessionCreated)
                        {
                            sessionCreated = true;
                        }

                        return ValueTask.CompletedTask;
                    },
                    InputTranscriptHandler = evt =>
                    {
                        inputTranscript.Append(evt.Delta);
                        return ValueTask.CompletedTask;
                    },
                    OutputTranscriptHandler = evt =>
                    {
                        outputTranscript.Append(evt.Delta);
                        return ValueTask.CompletedTask;
                    },
                    OutputAudioHandler = evt =>
                    {
                        if (evt.AudioData is { Length: > 0 })
                        {
                            outputAudioChunks.Add(evt.AudioData);
                        }

                        return ValueTask.CompletedTask;
                    },
                    SessionClosedHandler = _ =>
                    {
                        sessionClosed = true;
                        return ValueTask.CompletedTask;
                    },
                    ErrorHandler = evt =>
                    {
                        Assert.Fail($"Realtime translation error: {evt.Error?.Message ?? evt.RawJson}");
                        return ValueTask.CompletedTask;
                    }
                }
            });

        await session.AppendAudioStreamAsync(pcm16);
        await Task.Delay(1500);
        await session.CloseAsync();

        Assert.That(sessionCreated, Is.True, "Expected session.created");
        Assert.That(sessionClosed, Is.True, "Expected session.closed");
        Assert.That(outputAudioChunks, Is.Not.Empty, "Expected translated output audio deltas");
        Assert.That(outputTranscript.ToString(), Is.Not.Empty, "Expected translated transcript deltas");
        TestContext.WriteLine($"Input transcript: {inputTranscript}");
        TestContext.WriteLine($"Output transcript: {outputTranscript}");
        TestContext.WriteLine($"Output audio chunks: {outputAudioChunks.Count}");
    }

    [Test]
    public void RealtimeTranslationEventParser_MapsKnownTypes()
    {
        RealtimeTranslationEvent evt = VendorOpenAiRealtimeTranslation.ParseEvent("""
            {
              "type": "session.output_transcript.delta",
              "event_id": "evt_1",
              "delta": "Hola"
            }
            """);

        Assert.That(evt.EventType, Is.EqualTo(RealtimeTranslationEventTypes.OutputTranscriptDelta));
        Assert.That(evt.Delta, Is.EqualTo("Hola"));
    }

    private static async Task<byte[]> GenerateEnglishPcm16Async(TornadoApi api)
    {
        SpeechTtsResult? speech = await api.Audio.CreateSpeech(new SpeechRequest
        {
            Model = AudioModel.OpenAi.Gpt4.Gpt4OMiniTts,
            Input = "Hello, this is a realtime translation integration test.",
            Voice = SpeechVoice.Alloy,
            ResponseFormat = SpeechResponseFormat.Wav
        });

        Assert.That(speech, Is.Not.Null);
        Assert.That(speech!.Data, Is.Not.Empty);

        return ExtractPcm16FromWav(speech.Data);
    }

    private static byte[] ExtractPcm16FromWav(byte[] wavData)
    {
        if (wavData.Length <= 44)
        {
            return wavData;
        }

        int dataOffset = 12;
        while (dataOffset + 8 <= wavData.Length)
        {
            string chunkId = Encoding.ASCII.GetString(wavData, dataOffset, 4);
            int chunkSize = BitConverter.ToInt32(wavData, dataOffset + 4);

            if (chunkId == "data")
            {
                byte[] pcm = new byte[chunkSize];
                Buffer.BlockCopy(wavData, dataOffset + 8, pcm, 0, chunkSize);
                return pcm;
            }

            dataOffset += 8 + chunkSize;
        }

        return wavData[44..];
    }
}
