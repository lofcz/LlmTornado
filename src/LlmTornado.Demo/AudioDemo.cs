using LlmTornado.Audio;
using LlmTornado.Audio.Models;
using LlmTornado.Code;

namespace LlmTornado.Demo;

public class AudioDemo : DemoBase
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
    public static async Task TranscribeFormatTextMistral()
    {
        byte[] audioData = await File.ReadAllBytesAsync("Static/Audio/sample.wav");

        TranscriptionResult? transcription = await Program.Connect().Audio.CreateTranscription(new TranscriptionRequest
        {
            File = new AudioFile(audioData, AudioFileTypes.Wav),
            Model = AudioModel.Mistral.Free.VoxtralMini2507,
            ResponseFormat = AudioTranscriptionResponseFormats.Text
        });

        if (transcription is not null)
        {
            Console.WriteLine(transcription.Text);
        }
    }
    
    [TornadoTest]
    public static async Task TranscribeFormatTextStreaming()
    {
        byte[] audioData = await File.ReadAllBytesAsync("Static/Audio/sample.wav");

        await Program.Connect().Audio.StreamTranscriptionRich(new TranscriptionRequest
        {
            File = new AudioFile(audioData, AudioFileTypes.Wav),
            Model = AudioModel.OpenAi.Gpt4.Gpt4OTranscribe,
            ResponseFormat = AudioTranscriptionResponseFormats.Text
        }, new TranscriptionStreamEventHandler
        {
            ChunkHandler = (chunk) =>
            {
                Console.Write(chunk);
                return ValueTask.CompletedTask;
            },
            BlockHandler = (block) =>
            {
                Console.WriteLine();
                return ValueTask.CompletedTask;
            }
        });
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
    public static async Task TranscribeFormatJsonLogprobs()
    {
        byte[] audioData = await File.ReadAllBytesAsync("Static/Audio/sample.wav");

        TranscriptionResult? transcription = await Program.Connect().Audio.CreateTranscription(new TranscriptionRequest
        {
            File = new AudioFile(audioData, AudioFileTypes.Wav),
            Model = AudioModel.OpenAi.Gpt4.Gpt4OTranscribe,
            ResponseFormat = AudioTranscriptionResponseFormats.Json,
            Include = [ TranscriptionRequestIncludeItems.Logprobs ]
        });

        if (transcription is not null)
        {
            Console.WriteLine("Transcript");
            Console.WriteLine("--------------------------");
            
            Console.WriteLine(transcription.Text);
            Console.WriteLine();
            
            Console.WriteLine("Logprobs");
            Console.WriteLine("--------------------------");

            if (transcription.Logprobs is not null)
            {
                foreach (TranscriptionLogprob logprob in transcription.Logprobs)
                {
                    Console.WriteLine(logprob);
                }   
            }
        }
    }
    
    [TornadoTest]
    public static async Task TranscribeFormatJsonTimestamps()
    {
        byte[] audioData = await File.ReadAllBytesAsync("Static/Audio/ttsin.mp3");

        TranscriptionResult? transcription = await Program.Connect().Audio.CreateTranscription(new TranscriptionRequest
        {
            File = new AudioFile(audioData, AudioFileTypes.Wav),
            Model = AudioModel.OpenAi.Whisper.V2,
            ResponseFormat = AudioTranscriptionResponseFormats.VerboseJson,
            TimestampGranularities = [ TimestampGranularities.Segment, TimestampGranularities.Word ]
        });

        if (transcription is not null)
        {
            Console.WriteLine("Transcript");
            Console.WriteLine("--------------------------");
            
            Console.WriteLine(transcription.Text);
            Console.WriteLine();
            
            Console.WriteLine("Segments");
            Console.WriteLine("--------------------------");

            foreach (TranscriptionSegment segment in transcription.Segments)
            {
                Console.WriteLine(segment);
            }
            
            Console.WriteLine();
            Console.WriteLine("Words");
            Console.WriteLine("--------------------------");
            
            foreach (TranscriptionWord word in transcription.Words)
            {
                Console.WriteLine(word);
            }
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