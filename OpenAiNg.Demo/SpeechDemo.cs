using OpenAiNg.Audio;

namespace OpenAiNg.Demo;

public static class SpeechDemo
{
    public static async Task Tts()
    {
        SpeechTtsResult? result = await Program.Connect().Audio.CreateSpeechAsync(new SpeechRequest
        {
            Input = "Hi, how are you?",
            Model = Models.Model.TTS_1_HD,
            ResponseFormat = SpeechResponseFormat.Mp3,
            Voice = SpeechVoice.Alloy
        });

        if (result is not null)
        {
            await result.SaveAndDispose("ttsdemo.mp3");
        }
        
        int z = 0;
    }
}