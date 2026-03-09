using LlmTornado.Code;
using LlmTornado.Common;
using LlmTornado.Videos;
using LlmTornado.Videos.Models;
using LlmTornado.Videos.Vendors.MiniMax;
using LlmTornado.Videos.Vendors.Zai;

namespace LlmTornado.Demo;

public class VideoDemo : DemoBase
{
    [TornadoTest, Flaky("expensive")]
    public static async Task GenerateSimpleVideoGoogle()
    {
        TornadoApi api = Program.Connect();
        
        byte[] bytes = await File.ReadAllBytesAsync("Static/Images/bull.jpeg");
        string base64 = $"{Convert.ToBase64String(bytes)}";

        
        VideoGenerationRequest request = new VideoGenerationRequest(
            "A bull moves confidently forward, smiling and waving.",
            VideoModel.Google.Veo.V31Fast,
            duration: VideoDuration.Seconds8,
            aspectRatio: VideoAspectRatio.Widescreen,
            resolution: VideoResolution.HD
        )
        {
            Image = new VideoImage(base64, "image/jpeg")
        };
        
        const string outputPath = "output/generated_video.mp4";
        
        Console.WriteLine("Starting video generation...");
        HttpCallResult<VideoJob>? result = await api.Videos.CreateAndWait(request, new VideoJobEvents
        {
            OnPoll = async (result, index, elapsed) =>
            {
                Console.WriteLine(result.Progress is not null ? $"[Poll #{index}] Progress: {result.Progress}% - Elapsed: {elapsed.TotalSeconds:F1}s" : $"[Poll #{index}] Status: {(result?.Done == true ? "Done" : "In Progress")} - Elapsed: {elapsed.TotalSeconds:F1}s");
                await ValueTask.CompletedTask;
            },
            OnFinished = async (result, videoStream) =>
            {
                Console.WriteLine($"Video generation completed!");
                string savedTo = await videoStream.SaveToFileAsync(outputPath);
                Console.WriteLine($"Video saved to: {savedTo}");
            }
        });

        Console.WriteLine(result.Data?.Done == true ? $"Process completed. Check {outputPath} for the video." : "Video generation failed or returned no results.");
    }
    
    [TornadoTest, Flaky("expensive")]
    public static async Task GenerateSimpleVideoOpenAi()
    {
        TornadoApi api = Program.Connect();
        
        byte[] bytes = await File.ReadAllBytesAsync("Static/Images/bull_hd.jpeg");
        string base64 = $"{Convert.ToBase64String(bytes)}";

        
        VideoGenerationRequest request = new VideoGenerationRequest(
            "A bull moves confidently forward, smiling and waving.",
            VideoModel.OpenAi.Sora.Sora2,
            duration: VideoDuration.Seconds4,
            aspectRatio: VideoAspectRatio.Widescreen,
            resolution: VideoResolution.HD
        )
        {
            Image = new VideoImage(base64, "image/jpeg")
        };
        
        const string outputPath = "output/generated_video.mp4";
        
        Console.WriteLine("Starting video generation...");
        HttpCallResult<VideoJob>? result = await api.Videos.CreateAndWait(request, new VideoJobEvents
        {
            OnPoll = async (result, index, elapsed) =>
            {
                Console.WriteLine(result.Progress is not null ? $"[Poll #{index}] Progress: {result.Progress}% - Elapsed: {elapsed.TotalSeconds:F1}s" : $"[Poll #{index}] Status: {(result?.Done == true ? "Done" : "In Progress")} - Elapsed: {elapsed.TotalSeconds:F1}s");
                await ValueTask.CompletedTask;
            },
            OnFinished = async (result, videoStream) =>
            {
                Console.WriteLine($"Video generation completed!");
                string savedTo = await videoStream.SaveToFileAsync(outputPath);
                Console.WriteLine($"Video saved to: {savedTo}");
            }
        });

        Console.WriteLine(result.Data?.Done == true ? $"Process completed. Check {outputPath} for the video." : "Video generation failed or returned no results.");
    }
    
    [TornadoTest, Flaky("expensive")]
    public static async Task GenerateSimpleVideoXAi()
    {
        TornadoApi api = Program.Connect();
        
        VideoGenerationRequest request = new VideoGenerationRequest(
            "A cat playing with a ball in a sunlit garden.",
            VideoModel.XAi.Grok.ImagineVideo,
            duration: VideoDuration.Seconds6,
            aspectRatio: VideoAspectRatio.Widescreen,
            resolution: VideoResolution.HD
        );
        
        const string outputPath = "output/generated_video_xai.mp4";
        
        Console.WriteLine("Starting xAI video generation...");
        HttpCallResult<VideoJob>? result = await api.Videos.CreateAndWait(request, new VideoJobEvents
        {
            OnPoll = async (job, index, elapsed) =>
            {
                Console.WriteLine($"[Poll #{index}] Status: {job.Status} - Elapsed: {elapsed.TotalSeconds:F1}s");
                await ValueTask.CompletedTask;
            },
            OnFinished = async (job, videoStream) =>
            {
                Console.WriteLine($"Video generation completed!");
                string savedTo = await videoStream.SaveToFileAsync(outputPath);
                Console.WriteLine($"Video saved to: {savedTo}");
            }
        });

        Console.WriteLine(result.Data?.Done == true ? $"Process completed. Check {outputPath} for the video." : "Video generation failed or returned no results.");
    }
    
    [TornadoTest, Flaky("expensive")]
    public static async Task GenerateImageToVideoXAi()
    {
        TornadoApi api = Program.Connect();
        
        byte[] bytes = await File.ReadAllBytesAsync("Static/Images/bull.jpeg");
        string base64 = $"data:image/jpeg;base64,{Convert.ToBase64String(bytes)}";
        
        VideoGenerationRequest request = new VideoGenerationRequest(
            "A majestic bull walks forward confidently through a green meadow.",
            VideoModel.XAi.Grok.ImagineVideo,
            duration: VideoDuration.Seconds8,
            aspectRatio: VideoAspectRatio.Widescreen,
            resolution: VideoResolution.HD
        )
        {
            Image = new VideoImage(base64, "image/jpeg")
        };
        
        const string outputPath = "output/generated_video_xai_i2v.mp4";
        
        Console.WriteLine("Starting xAI image-to-video generation...");
        HttpCallResult<VideoJob>? result = await api.Videos.CreateAndWait(request, new VideoJobEvents
        {
            OnPoll = async (job, index, elapsed) =>
            {
                Console.WriteLine($"[Poll #{index}] Status: {job.Status} - Elapsed: {elapsed.TotalSeconds:F1}s");
                await ValueTask.CompletedTask;
            },
            OnFinished = async (job, videoStream) =>
            {
                Console.WriteLine($"Video generation completed!");
                string savedTo = await videoStream.SaveToFileAsync(outputPath);
                Console.WriteLine($"Video saved to: {savedTo}");
            }
        });

        Console.WriteLine(result.Data?.Done == true ? $"Process completed. Check {outputPath} for the video." : "Video generation failed or returned no results.");
    }
    
    [TornadoTest, Flaky("expensive")]
    public static async Task EditVideoXAi()
    {
        TornadoApi api = Program.Connect();
        
        // Note: The input video URL must be a direct, publicly accessible link.
        // Maximum supported video length is 8.7 seconds.
        const string inputVideoUrl = "https://example.com/your-video.mp4"; // Replace with actual video URL
        
        const string outputPath = "output/edited_video_xai.mp4";
        
        Console.WriteLine("Starting xAI video editing...");
        HttpCallResult<VideoJob>? result = await api.Videos.EditAndWait(
            prompt: "Make the colors more vibrant and add a cinematic look.",
            videoUrl: inputVideoUrl
        );

        if (result.Ok && result.Data?.Done == true && result.Data.VideoUri is not null)
        {
            StreamResponse? stream = await api.Videos.DownloadContent(result.Data);
            if (stream is not null)
            {
                VideoStream videoStream = new VideoStream(stream.Stream);
                string savedTo = await videoStream.SaveToFileAsync(outputPath);
                Console.WriteLine($"Edited video saved to: {savedTo}");
                await videoStream.DisposeAsync();
            }
        }
        else
        {
            Console.WriteLine("Video editing failed or returned no results.");
        }
    }
    
    [TornadoTest, Flaky("expensive")]
    public static async Task GenerateSimpleVideoZai()
    {
        TornadoApi api = Program.Connect();
        
        VideoGenerationRequest request = new VideoGenerationRequest(
            "A cat playing with a ball in a sunlit garden.",
            VideoModel.Zai.CogVideoX.V3,
            duration: VideoDuration.Seconds5,
            aspectRatio: VideoAspectRatio.Widescreen,
            resolution: VideoResolution.FullHD
        )
        {
            ZaiExtensions = new VideoZaiExtensions
            {
                Quality = VideoZaiQuality.Quality,
                WithAudio = true,
                Fps = 30
            }
        };
        
        const string outputPath = "output/generated_video_zai.mp4";
        
        Console.WriteLine("Starting Z.AI CogVideoX video generation...");
        HttpCallResult<VideoJob>? result = await api.Videos.CreateAndWait(request, new VideoJobEvents
        {
            OnPoll = async (job, index, elapsed) =>
            {
                Console.WriteLine($"[Poll #{index}] Status: {job.Status} - Elapsed: {elapsed.TotalSeconds:F1}s");
                await ValueTask.CompletedTask;
            },
            OnFinished = async (job, videoStream) =>
            {
                Console.WriteLine($"Video generation completed!");
                if (job.CoverImageUri is not null)
                {
                    Console.WriteLine($"Cover image available at: {job.CoverImageUri}");
                }
                string savedTo = await videoStream.SaveToFileAsync(outputPath);
                Console.WriteLine($"Video saved to: {savedTo}");
            }
        });

        Console.WriteLine(result.Data?.Done == true ? $"Process completed. Check {outputPath} for the video." : "Video generation failed or returned no results.");
    }
    
    [TornadoTest, Flaky("expensive")]
    public static async Task GenerateImageToVideoZai()
    {
        TornadoApi api = Program.Connect();
        
        byte[] bytes = await File.ReadAllBytesAsync("Static/Images/bull.jpeg");
        string base64 = $"data:image/jpeg;base64,{Convert.ToBase64String(bytes)}";
        
        VideoGenerationRequest request = new VideoGenerationRequest(
            "A majestic bull walks forward confidently through a green meadow.",
            VideoModel.Zai.Vidu.Q1Image,
            duration: VideoDuration.Seconds5
        )
        {
            Image = new VideoImage(base64, "image/jpeg"),
            ZaiExtensions = new VideoZaiExtensions
            {
                MovementAmplitude = VideoZaiMovementAmplitude.Medium,
                WithAudio = true
            }
        };
        
        const string outputPath = "output/generated_video_zai_i2v.mp4";
        
        Console.WriteLine("Starting Z.AI Vidu image-to-video generation...");
        HttpCallResult<VideoJob>? result = await api.Videos.CreateAndWait(request, new VideoJobEvents
        {
            OnPoll = async (job, index, elapsed) =>
            {
                Console.WriteLine($"[Poll #{index}] Status: {job.Status} - Elapsed: {elapsed.TotalSeconds:F1}s");
                await ValueTask.CompletedTask;
            },
            OnFinished = async (job, videoStream) =>
            {
                Console.WriteLine($"Video generation completed!");
                if (job.CoverImageUri is not null)
                {
                    Console.WriteLine($"Cover image available at: {job.CoverImageUri}");
                }
                string savedTo = await videoStream.SaveToFileAsync(outputPath);
                Console.WriteLine($"Video saved to: {savedTo}");
            }
        });

        Console.WriteLine(result.Data?.Done == true ? $"Process completed. Check {outputPath} for the video." : "Video generation failed or returned no results.");
    }
    
    [TornadoTest, Flaky("expensive")]
    public static async Task GenerateAnimeVideoZai()
    {
        TornadoApi api = Program.Connect();
        
        VideoGenerationRequest request = new VideoGenerationRequest(
            "A young warrior stands on a cliff, wind blowing through their hair, looking at a distant castle.",
            VideoModel.Zai.Vidu.Q1Text,
            duration: VideoDuration.Seconds5,
            aspectRatio: VideoAspectRatio.Widescreen
        )
        {
            ZaiExtensions = new VideoZaiExtensions
            {
                Style = VideoZaiStyle.Anime,
                MovementAmplitude = VideoZaiMovementAmplitude.Medium
            }
        };
        
        const string outputPath = "output/generated_video_zai_anime.mp4";
        
        Console.WriteLine("Starting Z.AI Vidu anime video generation...");
        HttpCallResult<VideoJob>? result = await api.Videos.CreateAndWait(request, new VideoJobEvents
        {
            OnPoll = async (job, index, elapsed) =>
            {
                Console.WriteLine($"[Poll #{index}] Status: {job.Status} - Elapsed: {elapsed.TotalSeconds:F1}s");
                await ValueTask.CompletedTask;
            },
            OnFinished = async (job, videoStream) =>
            {
                Console.WriteLine($"Video generation completed!");
                string savedTo = await videoStream.SaveToFileAsync(outputPath);
                Console.WriteLine($"Video saved to: {savedTo}");
            }
        });

        Console.WriteLine(result.Data?.Done == true ? $"Process completed. Check {outputPath} for the video." : "Video generation failed or returned no results.");
    }
    
    [TornadoTest, Flaky("expensive")]
    public static async Task GenerateSimpleVideoMiniMax()
    {
        TornadoApi api = Program.Connect();
        
        VideoGenerationRequest request = new VideoGenerationRequest(
            "A cat playing with a ball in a sunlit garden. [Tracking shot]",
            VideoModel.MiniMax.Hailuo.Hailuo23,
            duration: VideoDuration.Seconds6
        )
        {
            MiniMaxExtensions = new VideoMiniMaxExtensions
            {
                Resolution = VideoMiniMaxResolution.P1080,
                PromptOptimizer = true
            }
        };
        
        const string outputPath = "output/generated_video_minimax.mp4";
        
        Console.WriteLine("Starting MiniMax Hailuo video generation...");
        HttpCallResult<VideoJob>? result = await api.Videos.CreateAndWait(request, new VideoJobEvents
        {
            OnPoll = async (job, index, elapsed) =>
            {
                Console.WriteLine($"[Poll #{index}] Status: {job.Status} - Elapsed: {elapsed.TotalSeconds:F1}s");
                await ValueTask.CompletedTask;
            },
            OnFinished = async (job, videoStream) =>
            {
                Console.WriteLine($"Video generation completed! Size: {job.Size}");
                string savedTo = await videoStream.SaveToFileAsync(outputPath);
                Console.WriteLine($"Video saved to: {savedTo}");
            }
        });

        Console.WriteLine(result.Data?.Done == true ? $"Process completed. Check {outputPath} for the video." : "Video generation failed or returned no results.");
    }
    
    [TornadoTest, Flaky("expensive")]
    public static async Task GenerateImageToVideoMiniMax()
    {
        TornadoApi api = Program.Connect();
        
        byte[] bytes = await File.ReadAllBytesAsync("Static/Images/bull.jpeg");
        string base64 = $"data:image/jpeg;base64,{Convert.ToBase64String(bytes)}";
        
        VideoGenerationRequest request = new VideoGenerationRequest(
            "A majestic bull walks forward confidently through a green meadow.",
            VideoModel.MiniMax.Hailuo.Hailuo23,
            duration: VideoDuration.Seconds6
        )
        {
            Image = new VideoImage(base64, "image/jpeg"),
            MiniMaxExtensions = new VideoMiniMaxExtensions
            {
                Resolution = VideoMiniMaxResolution.P768,
                PromptOptimizer = false
            }
        };
        
        const string outputPath = "output/generated_video_minimax_i2v.mp4";
        
        Console.WriteLine("Starting MiniMax Hailuo image-to-video generation...");
        HttpCallResult<VideoJob>? result = await api.Videos.CreateAndWait(request, new VideoJobEvents
        {
            OnPoll = async (job, index, elapsed) =>
            {
                Console.WriteLine($"[Poll #{index}] Status: {job.Status} - Elapsed: {elapsed.TotalSeconds:F1}s");
                await ValueTask.CompletedTask;
            },
            OnFinished = async (job, videoStream) =>
            {
                Console.WriteLine($"Video generation completed! Size: {job.Size}");
                string savedTo = await videoStream.SaveToFileAsync(outputPath);
                Console.WriteLine($"Video saved to: {savedTo}");
            }
        });

        Console.WriteLine(result.Data?.Done == true ? $"Process completed. Check {outputPath} for the video." : "Video generation failed or returned no results.");
    }
}