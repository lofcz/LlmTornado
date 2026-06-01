using System;
using System.IO;
using System.Threading.Tasks;
using LlmTornado.Common;
using LlmTornado.Code;
using LlmTornado.Videos;
using LlmTornado.Videos.Models;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace LlmTornado.Tests.Videos;

[TestFixture]
[Category("Integration")]
public class VideoEditIntegrationTests
{
    private TornadoApi _api = null!;

    [SetUp]
    public void Setup()
    {
        string? apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");

        if (string.IsNullOrEmpty(apiKey))
        {
            string apiKeyPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "apiKey.json");
            if (File.Exists(apiKeyPath))
            {
                JObject keys = JObject.Parse(File.ReadAllText(apiKeyPath));
                apiKey = keys["OpenAi"]?.ToString();
            }
        }

        if (string.IsNullOrEmpty(apiKey))
        {
            Assert.Ignore("OPENAI_API_KEY environment variable or apiKey.json OpenAi key not set.");
        }

        _api = new TornadoApi(LLmProviders.OpenAi, apiKey);
    }

    [Test]
    [Explicit("Requires OpenAI API key and makes real Sora API calls")]
    public async Task Edit_CompletedVideo_ReturnsQueuedJob()
    {
        VideoGenerationRequest createRequest = new VideoGenerationRequest(
            "A red ball rolling slowly across a wooden floor.",
            VideoModel.OpenAi.Sora.Sora2,
            duration: VideoDuration.Seconds4,
            aspectRatio: VideoAspectRatio.Widescreen,
            resolution: VideoResolution.HD
        );

        HttpCallResult<VideoJob> createResult = await _api.Videos.CreateAndWait(
            createRequest,
            pollingIntervalMs: 5000,
            maxWaitMs: 600000
        );

        Assert.That(createResult.Ok, Is.True, createResult.Response);
        Assert.That(createResult.Data, Is.Not.Null);
        Assert.That(createResult.Data!.Status, Is.EqualTo(VideoJobStatus.Completed), createResult.Response);

        HttpCallResult<VideoJob> editResult = await _api.Videos.Edit(new VideoEditRequest(
            createResult.Data.Id,
            "Shift the ball color to deep blue while keeping the same motion."
        ));

        Assert.That(editResult.Ok, Is.True, editResult.Response);
        Assert.That(editResult.Data, Is.Not.Null);
        Assert.That(editResult.Data!.Id, Is.Not.Empty);
        Assert.That(editResult.Data.Status, Is.AnyOf(VideoJobStatus.Queued, VideoJobStatus.InProgress));
        Assert.That(editResult.Data.SourceProvider, Is.EqualTo(LLmProviders.OpenAi));
    }

    [Test]
    [Explicit("Requires OpenAI API key and makes real Sora API calls")]
    public async Task Remix_DelegatesToEditEndpoint()
    {
        VideoGenerationRequest createRequest = new VideoGenerationRequest(
            "A yellow sunflower swaying gently in the breeze.",
            VideoModel.OpenAi.Sora.Sora2,
            duration: VideoDuration.Seconds4,
            aspectRatio: VideoAspectRatio.Widescreen,
            resolution: VideoResolution.HD
        );

        HttpCallResult<VideoJob> createResult = await _api.Videos.CreateAndWait(
            createRequest,
            pollingIntervalMs: 5000,
            maxWaitMs: 600000
        );

        Assert.That(createResult.Ok, Is.True, createResult.Response);
        Assert.That(createResult.Data?.Status, Is.EqualTo(VideoJobStatus.Completed), createResult.Response);

#pragma warning disable CS0618
        HttpCallResult<VideoJob> remixResult = await _api.Videos.Remix(
            createResult.Data!.Id,
            "Make the petals orange instead of yellow."
        );
#pragma warning restore CS0618

        Assert.That(remixResult.Ok, Is.True, remixResult.Response);
        Assert.That(remixResult.Data, Is.Not.Null);
        Assert.That(remixResult.Data!.Id, Is.Not.Empty);
        Assert.That(remixResult.Data.Status, Is.AnyOf(VideoJobStatus.Queued, VideoJobStatus.InProgress));
    }
}
