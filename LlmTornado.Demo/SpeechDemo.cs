using LlmTornado.Audio;
using LlmTornado.Audio.Models;
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
            Model = AudioModel.OpenAi.Gpt4.Gpt4OMiniTts,
            ResponseFormat = SpeechResponseFormat.Mp3,
            Voice = SpeechVoice.Alloy,
            Instructions = "You are a very sad, tired person."
        });

        if (result is not null)
        {
            await result.SaveAndDispose("ttsdemo.mp3");
        }

        int z = 0;
    }
}