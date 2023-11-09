using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using OpenAiNg.Code;

namespace OpenAiNg.Audio;

public class SpeechTtsResult : ApiResultBase
{
    internal SpeechTtsResult(StreamResponse response)
    {
        AudioStream = response.Stream;
        Headers = response.Headers;
        Response = response.Response;
    } 
    
    /// <summary>
    /// The stream containing speech in the requested format - <see cref="SpeechResponseFormat"/>
    /// You are responsible for disposing of this stream! Use helper method <see cref="SaveAndDispose"/> to do that automatically
    /// </summary>
    public Stream AudioStream { get; set; }
    /// <summary>
    /// Metadata about usage and other information parsed from the response headers
    /// </summary>
    public ApiResultBase Headers { get; set; }
    /// <summary>
    /// Http response, you are responsible for disposing of this! Use helper method <see cref="SaveAndDispose"/> to do that automatically
    /// </summary>
    public HttpResponseMessage Response { get; set; }

    /// <summary>
    /// Saves the created speech in a given folder and disposes the <see cref="AudioStream"/>
    /// </summary>
    /// <param name="path">The folder to store speech in, this is passed to <see cref="File.Create(string)"/></param>
    /// <param name="createDirectory">Whether co call <see cref="Directory.CreateDirectory(path)"/></param>
    public async Task SaveAndDispose(string path, bool createDirectory = true)
    {
        if (createDirectory)
        {
            string? str = Path.GetDirectoryName(path);

            if (!string.IsNullOrWhiteSpace(str))
            {
                Directory.CreateDirectory(str);   
            }
        }
        
        await using FileStream fileStream = File.Create(path);

        if (AudioStream.CanSeek)
        {
            AudioStream.Seek(0, SeekOrigin.Begin);    
        }
        
        await AudioStream.CopyToAsync(fileStream);
        await AudioStream.DisposeAsync();
        Response.Dispose();
    }
}