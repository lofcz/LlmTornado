using LlmTornado.Audio;
using LlmTornado.Audio.Models;
using LlmTornado.Code;

namespace LlmTornado.Demo;

public static class TranscriptionDemo
{
    [TornadoTest]
    public static async Task TranscribeFormatText()
    {
        byte[] audioData = await File.ReadAllBytesAsync("Static/Audio/sample.wav");

        TranscriptionResult? transcription = await Program.Connect().Audio.CreateTranscription(new TranscriptionRequest
        {
            File = new AudioFile(audioData, AudioFileTypes.Wav),
            Model = AudioModel.OpenAi.Whisper.V2,
            ResponseFormat = AudioTranscriptionResponseFormats.Text
        });

        if (transcription is not null)
        {
            Console.WriteLine(transcription.Text);
        }
    }
    
    [TornadoTest]
    public static async Task TranscribeFormatJson()
    {
        byte[] audioData = await File.ReadAllBytesAsync("Static/Audio/sample.wav");

        TranscriptionResult? transcription = await Program.Connect().Audio.CreateTranscription(new TranscriptionRequest
        {
            File = new AudioFile(audioData, AudioFileTypes.Wav),
            Model = AudioModel.OpenAi.Whisper.V2,
            ResponseFormat = AudioTranscriptionResponseFormats.Json
        });

        if (transcription is not null)
        {
            Console.WriteLine(transcription.Text);
        }
    }
    
    [TornadoTest]
    public static async Task TranscribeFormatSrt()
    {
        byte[] audioData = await File.ReadAllBytesAsync("Static/Audio/sample.wav");

        TranscriptionResult? transcription = await Program.Connect().Audio.CreateTranscription(new TranscriptionRequest
        {
            File = new AudioFile(audioData, AudioFileTypes.Wav),
            Model = AudioModel.OpenAi.Whisper.V2,
            ResponseFormat = AudioTranscriptionResponseFormats.Srt
        });

        if (transcription is not null)
        {
            Console.WriteLine(transcription.Text);
        }
    }
    
    [TornadoTest]
    public static async Task TranscribeFormatJsonVerbose()
    {
        byte[] audioData = await File.ReadAllBytesAsync("Static/Audio/sample.wav");

        TranscriptionResult? transcription = await Program.Connect().Audio.CreateTranscription(new TranscriptionRequest
        {
            File = new AudioFile(audioData, AudioFileTypes.Wav),
            Model = AudioModel.OpenAi.Whisper.V2,
            ResponseFormat = AudioTranscriptionResponseFormats.VerboseJson
        });

        if (transcription is not null)
        {
            Console.WriteLine(transcription.Text);
        }
    }
    
    [TornadoTest]
    public static async Task TranscribeFormatJsonVerboseGroq()
    {
        byte[] audioData = await File.ReadAllBytesAsync("Static/Audio/sample.wav");

        TranscriptionResult? transcription = await Program.Connect().Audio.CreateTranscription(new TranscriptionRequest
        {
            File = new AudioFile(audioData, AudioFileTypes.Wav),
            Model = AudioModel.Groq.OpenAi.WhisperV3Turbo,
            ResponseFormat = AudioTranscriptionResponseFormats.VerboseJson
        });

        if (transcription is not null)
        {
            Console.WriteLine(transcription.Text);
        }
    }
}