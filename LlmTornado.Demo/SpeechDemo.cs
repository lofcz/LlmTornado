using LlmTornado.Audio;
using LlmTornado.Models;

namespace LlmTornado.Demo;

public static class SpeechDemo
{
    [TornadoTest]
    [Flaky("expensive")]
    public static async Task Tts()
    {
        SpeechTtsResult? result = await Program.Connect().Audio.CreateSpeech(new SpeechRequest
        {
            Input = "Hi, how are you?",
            Model = Model.TTS_1_HD,
            ResponseFormat = SpeechResponseFormat.Mp3,
            Voice = SpeechVoice.Alloy
        });

        if (result is not null) await result.SaveAndDispose("ttsdemo.mp3");

        int z = 0;
    }
}