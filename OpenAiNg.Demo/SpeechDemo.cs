using OpenAiNg.Audio;

namespace OpenAiNg.Demo;

public static class SpeechDemo
{
    public async static Task Tts()
    {
        SpeechTtsResult? result = await Program.Connect().Audio.CreateSpeechAsync(new SpeechRequest
        {
            Input = "Hi, how are you?",
            Model = Models.Model.TTS_1_HD,
            ResponseFormat = SpeechResponseFormat.Mp3,
            Voice = SpeechVoice.Alloy
        });

        int z = 0;
    }
}