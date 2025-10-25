using LlmTornado.Videos;
using LlmTornado.Videos.Models;
using LlmTornado.Videos.Vendors.Google;

namespace LlmTornado.Demo;

public class VideoDemo : DemoBase
{
    [TornadoTest, Flaky("expensive")]
    public static async Task GenerateSimpleVideoGoogle()
    {
        TornadoApi api = Program.Connect();
        
        VideoGenerationRequest request = new VideoGenerationRequest(
            "A lion is programming with a squirrel friend.",
            VideoModel.Google.Veo.V31,
            duration: VideoDuration.Seconds8,
            aspectRatio: VideoAspectRatio.Widescreen,
            resolution: VideoResolution.HD
        );
        
        const string outputPath = "output/generated_video.mp4";
        
        Console.WriteLine("Starting video generation...");
        VideoGenerationResult? result = await api.Videos.CreateVideoAndWait(request, new VideoGenerationEvents
        {
            OnPoll = async (result, index, elapsed) =>
            {
                Console.WriteLine(result?.Metadata?.ProgressPercent.HasValue == true ? $"[Poll #{index}] Progress: {result.Metadata.ProgressPercent}% - Elapsed: {elapsed.TotalSeconds:F1}s" : $"[Poll #{index}] Status: {(result?.Done == true ? "Done" : "In Progress")} - Elapsed: {elapsed.TotalSeconds:F1}s");
                await ValueTask.CompletedTask;
            },
            OnFinished = async (result, videoStream) =>
            {
                Console.WriteLine($"Video generation completed!");
                string savedTo = await videoStream.SaveToFileAsync(outputPath);
                Console.WriteLine($"Video saved to: {savedTo}");
            }
        });

        Console.WriteLine(result?.Done == true ? $"Process completed. Check {outputPath} for the video." : "Video generation failed or returned no results.");
    }
}