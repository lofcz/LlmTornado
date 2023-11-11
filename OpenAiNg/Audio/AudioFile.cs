using System.IO;

namespace OpenAiNg.Audio;

/// <summary>
///     Audio file object for transcript and translate requests.
/// </summary>
public class AudioFile
{
    /// <summary>
    ///     Stream of the file.
    /// </summary>
    public Stream File { get; set; }

    /// <summary>
    ///     Content length of the file
    /// </summary>
    public long ContentLength => File.Length;

    /// <summary>
    ///     Type of audio file. Must be 'audio/mp3', 'video/mp4', 'video/mpeg', 'audio/mpga', 'audio/m4a', 'audio/wav', or 'video/webm'.
    /// </summary>
    public string ContentType { get; set; }

    /// <summary>
    ///     Full name of the file. such as test.mp3
    /// </summary>
    public string Name { get; set; }
}