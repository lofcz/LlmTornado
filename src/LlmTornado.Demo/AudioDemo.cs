using LlmTornado.Audio;
using LlmTornado.Audio.Models;
using LlmTornado.Audio.Vendors.Zai;
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
    
    [TornadoTest]
    public static async Task TranscribeFormatDiarizedJson()
    {
        byte[] audioData = await File.ReadAllBytesAsync("Static/Audio/diarized.wav");

        TranscriptionResult? transcription = await Program.Connect().Audio.CreateTranscription(new TranscriptionRequest
        {
            File = new AudioFile(audioData, AudioFileTypes.Wav),
            Model = AudioModel.OpenAi.Gpt4.Gpt4OTranscribeDiarize,
            ResponseFormat = AudioTranscriptionResponseFormats.DiarizedJson
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
                Console.WriteLine($"[{segment.Start:F2} - {segment.End:F2}] Speaker {segment.Speaker}: {segment.Text}");
            }
        }
    }
    
    /// <summary>
    /// Transcribes audio using Z.AI's GLM-ASR-2512 model.
    /// Note: Z.AI requires mono channel audio. This demo converts stereo to mono if needed.
    /// </summary>
    [TornadoTest]
    public static async Task TranscribeZai()
    {
        byte[] audioData = await File.ReadAllBytesAsync("Static/Audio/sample.wav");
        
        // Z.AI requires mono channel audio - convert if stereo
        audioData = ConvertToMonoWavIfNeeded(audioData);

        TranscriptionResult? transcription = await Program.Connect().Audio.CreateTranscription(new TranscriptionRequest
        {
            File = new AudioFile(audioData, AudioFileTypes.Wav),
            Model = AudioModel.Zai.Asr.GlmAsr2512
        });

        if (transcription is not null)
        {
            Console.WriteLine(transcription.Text);
        }
    }
    
    /// <summary>
    /// Transcribes audio using Z.AI with hotwords for improved domain-specific vocabulary recognition.
    /// </summary>
    [TornadoTest]
    public static async Task TranscribeZaiWithHotwords()
    {
        byte[] audioData = await File.ReadAllBytesAsync("Static/Audio/sample.wav");
        
        // Z.AI requires mono channel audio - convert if stereo
        audioData = ConvertToMonoWavIfNeeded(audioData);

        TranscriptionResult? transcription = await Program.Connect().Audio.CreateTranscription(new TranscriptionRequest
        {
            File = new AudioFile(audioData, AudioFileTypes.Wav),
            Model = AudioModel.Zai.Asr.GlmAsr2512,
            ZaiExtensions = new TranscriptionRequestZaiExtensions
            {
                Hotwords = ["LlmTornado", "OpenAI", "transcription"]
            }
        });

        if (transcription is not null)
        {
            Console.WriteLine(transcription.Text);
        }
    }
    
    /// <summary>
    /// Streams transcription from Z.AI's GLM-ASR-2512 model.
    /// </summary>
    [TornadoTest]
    public static async Task TranscribeZaiStreaming()
    {
        byte[] audioData = await File.ReadAllBytesAsync("Static/Audio/sample.wav");
        
        // Z.AI requires mono channel audio - convert if stereo
        audioData = ConvertToMonoWavIfNeeded(audioData);

        await Program.Connect().Audio.StreamTranscriptionRich(new TranscriptionRequest
        {
            File = new AudioFile(audioData, AudioFileTypes.Wav),
            Model = AudioModel.Zai.Asr.GlmAsr2512
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
    
    /// <summary>
    /// Converts a stereo WAV file to mono by averaging left and right channels.
    /// Z.AI's transcription API only supports mono channel audio.
    /// </summary>
    private static byte[] ConvertToMonoWavIfNeeded(byte[] wavData)
    {
        // WAV header structure:
        // Bytes 0-3: "RIFF"
        // Bytes 4-7: File size - 8
        // Bytes 8-11: "WAVE"
        // Bytes 12-15: "fmt "
        // Bytes 16-19: Subchunk1 size (usually 16 for PCM)
        // Bytes 20-21: Audio format (1 = PCM)
        // Bytes 22-23: Number of channels
        // Bytes 24-27: Sample rate
        // Bytes 28-31: Byte rate
        // Bytes 32-33: Block align
        // Bytes 34-35: Bits per sample
        
        if (wavData.Length < 44)
        {
            return wavData; // Too small to be a valid WAV
        }
        
        // Check if it's a WAV file
        if (wavData[0] != 'R' || wavData[1] != 'I' || wavData[2] != 'F' || wavData[3] != 'F')
        {
            return wavData; // Not a WAV file
        }
        
        // Get number of channels (bytes 22-23, little-endian)
        int numChannels = wavData[22] | (wavData[23] << 8);
        
        if (numChannels == 1)
        {
            return wavData; // Already mono
        }
        
        if (numChannels != 2)
        {
            return wavData; // Not stereo, don't know how to handle
        }
        
        // Get bits per sample
        int bitsPerSample = wavData[34] | (wavData[35] << 8);
        int bytesPerSample = bitsPerSample / 8;
        
        // Get sample rate
        int sampleRate = wavData[24] | (wavData[25] << 8) | (wavData[26] << 16) | (wavData[27] << 24);
        
        // Find data chunk
        int dataOffset = 12;
        int dataSize = 0;
        
        while (dataOffset < wavData.Length - 8)
        {
            string chunkId = System.Text.Encoding.ASCII.GetString(wavData, dataOffset, 4);
            int chunkSize = wavData[dataOffset + 4] | (wavData[dataOffset + 5] << 8) | 
                           (wavData[dataOffset + 6] << 16) | (wavData[dataOffset + 7] << 24);
            
            if (chunkId == "data")
            {
                dataOffset += 8;
                dataSize = chunkSize;
                break;
            }
            
            dataOffset += 8 + chunkSize;
        }
        
        if (dataSize == 0)
        {
            return wavData; // Couldn't find data chunk
        }
        
        // Convert stereo to mono
        int numStereoSamples = dataSize / (bytesPerSample * 2);
        int monoDataSize = numStereoSamples * bytesPerSample;
        
        byte[] monoData = new byte[44 + monoDataSize];
        
        // Copy and modify header
        Array.Copy(wavData, 0, monoData, 0, 44);
        
        // Update file size (bytes 4-7)
        int newFileSize = 36 + monoDataSize;
        monoData[4] = (byte)(newFileSize & 0xFF);
        monoData[5] = (byte)((newFileSize >> 8) & 0xFF);
        monoData[6] = (byte)((newFileSize >> 16) & 0xFF);
        monoData[7] = (byte)((newFileSize >> 24) & 0xFF);
        
        // Update number of channels to 1 (bytes 22-23)
        monoData[22] = 1;
        monoData[23] = 0;
        
        // Update byte rate (bytes 28-31): SampleRate * NumChannels * BitsPerSample/8
        int newByteRate = sampleRate * 1 * bytesPerSample;
        monoData[28] = (byte)(newByteRate & 0xFF);
        monoData[29] = (byte)((newByteRate >> 8) & 0xFF);
        monoData[30] = (byte)((newByteRate >> 16) & 0xFF);
        monoData[31] = (byte)((newByteRate >> 24) & 0xFF);
        
        // Update block align (bytes 32-33): NumChannels * BitsPerSample/8
        int newBlockAlign = 1 * bytesPerSample;
        monoData[32] = (byte)(newBlockAlign & 0xFF);
        monoData[33] = (byte)((newBlockAlign >> 8) & 0xFF);
        
        // Update data chunk size (bytes 40-43)
        monoData[40] = (byte)(monoDataSize & 0xFF);
        monoData[41] = (byte)((monoDataSize >> 8) & 0xFF);
        monoData[42] = (byte)((monoDataSize >> 16) & 0xFF);
        monoData[43] = (byte)((monoDataSize >> 24) & 0xFF);
        
        // Convert audio samples - average left and right channels
        int monoOffset = 44;
        
        for (int i = 0; i < numStereoSamples; i++)
        {
            int stereoOffset = dataOffset + i * bytesPerSample * 2;
            
            if (bytesPerSample == 2) // 16-bit audio
            {
                // Read left and right samples as signed 16-bit
                short left = (short)(wavData[stereoOffset] | (wavData[stereoOffset + 1] << 8));
                short right = (short)(wavData[stereoOffset + 2] | (wavData[stereoOffset + 3] << 8));
                
                // Average the samples
                short mono = (short)((left + right) / 2);
                
                // Write mono sample
                monoData[monoOffset] = (byte)(mono & 0xFF);
                monoData[monoOffset + 1] = (byte)((mono >> 8) & 0xFF);
                monoOffset += 2;
            }
            else if (bytesPerSample == 1) // 8-bit audio
            {
                byte left = wavData[stereoOffset];
                byte right = wavData[stereoOffset + 1];
                
                // Average the samples
                byte mono = (byte)((left + right) / 2);
                
                monoData[monoOffset] = mono;
                monoOffset += 1;
            }
        }
        
        Console.WriteLine($"Converted stereo WAV to mono: {wavData.Length} bytes -> {monoData.Length} bytes");
        return monoData;
    }
    
    /// <summary>
    /// Transcribes audio using Groq with timestamp granularities for word-level timestamps.
    /// </summary>
    [TornadoTest]
    public static async Task TranscribeGroqWithTimestamps()
    {
        byte[] audioData = await File.ReadAllBytesAsync("Static/Audio/sample.wav");

        TranscriptionResult? transcription = await Program.Connect().Audio.CreateTranscription(new TranscriptionRequest
        {
            File = new AudioFile(audioData, AudioFileTypes.Wav),
            Model = AudioModel.Groq.OpenAi.WhisperV3,
            ResponseFormat = AudioTranscriptionResponseFormats.VerboseJson,
            TimestampGranularities = [TimestampGranularities.Word, TimestampGranularities.Segment]
        });

        if (transcription is not null)
        {
            Console.WriteLine("Transcript");
            Console.WriteLine("--------------------------");
            Console.WriteLine(transcription.Text);
            Console.WriteLine();
            
            if (transcription.Words?.Count > 0)
            {
                Console.WriteLine("Words");
                Console.WriteLine("--------------------------");
                foreach (var word in transcription.Words)
                {
                    Console.WriteLine($"[{word.Start:F2} - {word.End:F2}] {word.Word}");
                }
            }
        }
    }
    
    /// <summary>
    /// Translates audio to English using Groq.
    /// </summary>
    [TornadoTest]
    public static async Task TranslateGroq()
    {
        byte[] audioData = await File.ReadAllBytesAsync("Static/Audio/sample.wav");

        TranscriptionResult? translation = await Program.Connect().Audio.CreateTranslation(new TranslationRequest
        {
            File = new AudioFile(audioData, AudioFileTypes.Wav),
            Model = AudioModel.Groq.OpenAi.WhisperV3
        });

        if (translation is not null)
        {
            Console.WriteLine(translation.Text);
        }
    }
    
    /// <summary>
    /// Generates speech using Groq's Canopy Labs Orpheus TTS model.
    /// </summary>
    [TornadoTest]
    public static async Task SpeechGroq()
    {
        SpeechTtsResult? result = await Program.Connect().Audio.CreateSpeech(new SpeechRequest
        {
            Input = "Hello! I am speaking through Groq's Orpheus text-to-speech model. The speed of this inference is incredible!",
            Model = AudioModel.Groq.CanopyLabs.OrpheusV1English,
            Voice = SpeechVoice.AutumnOrpheus,
            ResponseFormat = SpeechResponseFormat.Wav,
            Speed = 1.0f,
            SampleRate = 24000
        });

        if (result is not null)
        {
            await using FileStream fs = new FileStream("groq_speech_output.wav", FileMode.Create);
            await result.AudioStream.CopyToAsync(fs);
            Console.WriteLine("Speech saved to groq_speech_output.wav");
        }
    }
    
    /// <summary>
    /// Generates speech using OpenAI's TTS model.
    /// </summary>
    [TornadoTest]
    public static async Task SpeechOpenAi()
    {
        SpeechTtsResult? result = await Program.Connect().Audio.CreateSpeech(new SpeechRequest
        {
            Input = "Hello! I am speaking through OpenAI's text-to-speech model. This is a demonstration of the audio synthesis capabilities.",
            Model = AudioModel.OpenAi.Tts.Tts1,
            Voice = SpeechVoice.Alloy,
            ResponseFormat = SpeechResponseFormat.Mp3,
            Speed = 1.0f
        });

        if (result is not null)
        {
            await using FileStream fs = new FileStream("openai_speech_output.mp3", FileMode.Create);
            await result.AudioStream.CopyToAsync(fs);
            Console.WriteLine("Speech saved to openai_speech_output.mp3");
        }
    }
    
    /// <summary>
    /// Generates lyrics for a song using MiniMax.
    /// </summary>
    [TornadoTest]
    [Flaky("expensive")]
    public static async Task GenerateLyricsMiniMax()
    {
        LyricsGenerationResult? result = await Program.ConnectMulti().Audio.GenerateLyrics(new LyricsGenerationRequest
        {
            Mode = LyricsGenerationMode.WriteFullSong,
            Prompt = "A cheerful love song about a summer day at the beach"
        });

        if (result is not null)
        {
            Console.WriteLine($"Title: {result.SongTitle}");
            Console.WriteLine($"Style: {result.StyleTags}");
            Console.WriteLine();
            Console.WriteLine(result.Lyrics);
        }
    }
    
    /// <summary>
    /// Generates music from lyrics using MiniMax.
    /// </summary>
    [TornadoTest]
    [Flaky("expensive")]
    public static async Task GenerateMusicMiniMax()
    {
        MusicGenerationResult? result = await Program.ConnectMulti().Audio.GenerateMusic(new MusicGenerationRequest
        {
            Model = AudioModel.MiniMax.Music.Music25,
            Prompt = "Indie folk, melancholic, introspective, longing, solitary walk, coffee shop",
            Lyrics = "[verse]\nStreetlights flicker, the night breeze sighs\nShadows stretch as I walk alone\nAn old coat wraps my silent sorrow\nWandering, longing, where should I go\n[chorus]\nPushing the wooden door, the aroma spreads\nIn a familiar corner, a stranger gazes",
            OutputFormat = MusicOutputFormat.Url,
            AudioSetting = new MusicAudioSetting
            {
                SampleRate = 44100,
                Bitrate = 256000,
                Format = MusicAudioFormat.Mp3
            }
        });

        if (result is not null)
        {
            Console.WriteLine($"Status: {(result.IsCompleted ? "Completed" : "In Progress")}");
            Console.WriteLine($"Duration: {result.DurationMs}ms");
            Console.WriteLine($"Sample Rate: {result.SampleRate}");
            Console.WriteLine($"Size: {result.Size} bytes");
            
            if (!string.IsNullOrEmpty(result.Audio))
            {
                Console.WriteLine($"Audio URL/Data length: {result.Audio.Length} chars");
                Console.WriteLine(result.Audio);
            }
        }
    }
}
