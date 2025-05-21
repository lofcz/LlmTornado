using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using EnumsNET;
using LlmTornado.ChatFunctions;
using LlmTornado.Code;
using NAudio.Lame;
using NAudio.Wave;
using Newtonsoft.Json.Linq;

namespace LlmTornado.Contrib;

public static class LlmTornadoExtensions
{
    /// <summary>
    /// Exports Audio to the specified format and returns the path to the exported file.<br/>
    /// Note: <see cref="ChatMessageAudio.Data"/>, <see cref="ChatMessageAudio.Format"/>, and <see cref="ChatMessageAudio.MimeType"/> must be all set.
    /// </summary>
    /// <param name="audio">The audio to export</param>
    /// <param name="targetFormat">The target format</param>
    /// <param name="outputPath">Optional output path. If not provided, a temporary file will be created.</param>
    /// <returns>Path to the exported audio file</returns>
    public static string? Export(this ChatMessageAudio audio, ChatAudioFormats targetFormat, string? outputPath = null)
    {
        if (audio.Data is null || audio.Format is null || audio.MimeType is null)
        {
            return null;
        }
        
        return new ChatAudio(audio.Data, audio.Format.Value, audio.MimeType).Export(targetFormat, outputPath);
    }
    
    /// <summary>
    /// Exports Audio to the specified format and returns the path to the exported file.
    /// </summary>
    /// <param name="audio">The audio to export</param>
    /// <param name="format">The target format</param>
    /// <param name="outputPath">Optional output path. If not provided, a temporary file will be created.</param>
    /// <returns>Path to the exported audio file</returns>
    public static string Export(this ChatAudio audio, ChatAudioFormats format, string? outputPath = null)
    {
        if (string.IsNullOrEmpty(outputPath))
        {
            string extension = format.ToString().ToLower();
            outputPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.{extension}");
        }
        
        byte[] sourceData = GetAudioData(audio);
        int sampleRate = GetSampleRate(audio);
        int channels = GetChannelCount(audio);

        string? tempWavPath = null;
        bool needsCleanup = false;

        try
        {
            switch (audio.Format)
            {
                case ChatAudioFormats.L16:
                {
                    tempWavPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.wav");
                    ConvertL16ToWav(sourceData, tempWavPath, sampleRate, channels);
                    break;
                }
                case ChatAudioFormats.Wav:
                {
                    tempWavPath = CreateTempFileWithData(sourceData, ".wav");
                    break;
                }
                default:
                {
                    tempWavPath = ConvertToWav(sourceData, audio);
                    break;
                }
            }

            needsCleanup = true;

            switch (format)
            {
                case ChatAudioFormats.Wav:
                    File.Copy(tempWavPath, outputPath, true);
                    break;

                case ChatAudioFormats.Mp3:
                    ConvertWavToMp3(tempWavPath, outputPath);
                    break;

                case ChatAudioFormats.L16:
                    ConvertWavToL16(tempWavPath, outputPath);
                    break;

                default:
                    throw new ArgumentException($"Unsupported format: {format}");
            }

            return outputPath;
        }
        finally
        {
            if (needsCleanup && tempWavPath != null && File.Exists(tempWavPath))
            {
                try
                {
                    File.Delete(tempWavPath);
                }
                catch
                {
                    
                }
            }
        }
    }

    private static byte[] GetAudioData(ChatAudio audio)
    {
        return Convert.FromBase64String(audio.Data);
    }

    private static int GetSampleRate(ChatAudio audio)
    {
        if (!string.IsNullOrEmpty(audio.MimeType) && audio.MimeType.Contains("rate="))
        {
            string rateStr = audio.MimeType.Split(["rate="], StringSplitOptions.None)[1].Split(';')[0];
            
            if (int.TryParse(rateStr, out int rate))
            {
                return rate;
            }
        }

        return 44100;
    }

    private static int GetChannelCount(ChatAudio audio)
    {
        return 1;
    }

    private static string CreateTempFileWithData(byte[] data, string extension)
    {
        string tempPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}{extension}");
        File.WriteAllBytes(tempPath, data);
        return tempPath;
    }

    private static string ConvertToWav(byte[] sourceData, ChatAudio audio)
    {
        string tempSourcePath = CreateTempFileWithData(sourceData, DetermineSourceExtension(audio));
        string tempWavPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.wav");

        try
        {
            using MediaFoundationReader reader = new MediaFoundationReader(tempSourcePath);
            using WaveFileWriter writer = new WaveFileWriter(tempWavPath, reader.WaveFormat);
            reader.CopyTo(writer);

            return tempWavPath;
        }
        finally
        {
            if (File.Exists(tempSourcePath))
            {
                try
                {
                    File.Delete(tempSourcePath);
                }
                catch
                {
                    
                }
            }
        }
    }

    private static string DetermineSourceExtension(ChatAudio audio)
    {
        return audio.Format switch
        {
            ChatAudioFormats.Mp3 => ".mp3",
            ChatAudioFormats.Wav => ".wav",
            _ => ".bin"
        };
    }

    private static void ConvertL16ToWav(byte[] audioData, string outputPath, int sampleRate, int channels)
    {
        using MemoryStream memoryStream = new MemoryStream();
        using BinaryWriter writer = new BinaryWriter(memoryStream);
        int bitsPerSample = 16;
        int blockAlign = channels * bitsPerSample / 8;
        int subchunk2Size = audioData.Length;
        int chunkSize = 36 + subchunk2Size;
        
        writer.Write("RIFF"u8.ToArray());
        writer.Write(chunkSize);
        writer.Write("WAVE"u8.ToArray());
        
        writer.Write("fmt "u8.ToArray());
        writer.Write(16);
        writer.Write((short)1);
        writer.Write((short)channels);
        writer.Write(sampleRate);
        writer.Write(sampleRate * blockAlign);
        writer.Write((short)blockAlign);
        writer.Write((short)bitsPerSample);
        
        writer.Write("data"u8.ToArray());
        writer.Write(subchunk2Size);
        writer.Write(audioData);
            
        File.WriteAllBytes(outputPath, memoryStream.ToArray());
    }

    private static void ConvertWavToMp3(string wavPath, string mp3Path)
    {
        using AudioFileReader reader = new AudioFileReader(wavPath);
        using LameMP3FileWriter writer = new LameMP3FileWriter(mp3Path, reader.WaveFormat, 128);
        reader.CopyTo(writer);
    }

    private static void ConvertWavToL16(string wavPath, string l16Path)
    {
        using WaveFileReader reader = new WaveFileReader(wavPath);

        if (reader.WaveFormat.Encoding != WaveFormatEncoding.Pcm)
        {
            throw new InvalidOperationException("Only PCM WAV files can be converted to L16");
        }
        
        if (reader.WaveFormat.BitsPerSample != 16)
        {
            throw new InvalidOperationException("Only 16-bit WAV files can be converted to L16");
        }
        
        byte[] buffer = new byte[reader.Length];
        reader.Read(buffer, 0, buffer.Length);
        File.WriteAllBytes(l16Path, buffer);
    }

    /// <summary>
    ///     Attempts to get value of a given argument. If the argument is of incompatible type, <see cref="exception"/> is returned.
    /// </summary>
    /// <param name="call"></param>
    /// <param name="arg"></param>
    /// <param name="data"></param>
    /// <param name="exception"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static bool TryGetArgument<T>(this FunctionCall call, string arg, [NotNullWhen(returnValue: true)] out T? data, out Exception? exception)
    {
        return Get(call.ArgGetter.Value.Source, arg, out data, out exception);
    }
    
    /// <summary>
    ///     Attempts to get value of a given argument. If the argument is not found or is of incompatible type, null is returned.
    /// </summary>
    /// <param name="call"></param>
    /// <param name="arg"></param>
    /// <param name="data"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static bool TryGetArgument<T>(this FunctionCall call, string arg, [NotNullWhen(returnValue: true)] out T? data)
    {
        return Get(call.ArgGetter.Value.Source, arg, out data, out _);
    }
    
    internal static bool Get<T>(this Dictionary<string,object?>? args, string param, out T? data, out Exception? exception)
    {
        exception = null;
        
        if (args is null)
        {
            data = default;
            return false; 
        }
        
        if (!args.TryGetValue(param, out object? rawData))
        {
            data = default;
            return false;
        }

        if (rawData is T obj)
        {
            data = obj;
        }

        if (rawData is JArray jArr)
        {
            data = jArr.ToObject<T?>();
            return true;
        }

        try
        {
            data = (T?)rawData.ChangeType(typeof(T));
            return true;
        }
        catch (Exception e)
        {
            data = default;
            exception = e;
            return false;
        }
    }
    
    public static object? ChangeType(this object? value, Type conversion)
    {
        Type? t = conversion;

        if (t.IsEnum && value != null)
        {
            if (Enums.TryParse(t, value.ToString(), true, out object? x))
            {
                return x;
            }
        }
        
        Type? nullableUnderlyingType = Nullable.GetUnderlyingType(t);
        
        if (nullableUnderlyingType is not null && value is not null)
        {
            if (nullableUnderlyingType.IsEnum)
            {
                if (Enums.TryParse(nullableUnderlyingType, value.ToString(), true, out object? x))
                {
                    return x;
                }
            }
        }
        
        if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            if (value == null)
            {
                return null;
            }

            t = Nullable.GetUnderlyingType(t);
        }

        if (t == typeof(int) && value?.ToString() == string.Empty)
        {
            return 0;
        }

        if (t == typeof(int) && ((value?.ToString()?.Contains('.') ?? false) || (value?.ToString()?.Contains(',') ?? false)))
        {
            if (double.TryParse(value.ToString()?.Replace(",", "."), out double x))
            {
                return (int)x;
            }
        }

        if (value is not null && t is { IsGenericType: true } && value.GetType().IsGenericType)
        {
            Type destT = t.GetGenericArguments()[0];
            Type sourceT = value.GetType().GetGenericArguments()[0];

            if (destT.IsEnum && sourceT == typeof(int))
            {
                IList? instance = (IList?)Activator.CreateInstance(t);

                foreach (object? x in (IList)value)
                {
                    instance?.Add(x);
                }

                return instance;
            }
        }

        return t is not null ? Convert.ChangeType(value, t) : null;
    }
}