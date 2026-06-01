using System;
using System.IO;
using System.Threading.Tasks;
using LlmTornado.Batch;
using LlmTornado.Code;
using LlmTornado.Common;
using LlmTornado.Videos;
using LlmTornado.Videos.Models;
using LlmTornado.Videos.Vendors.OpenAi;
using NUnit.Framework;

namespace LlmTornado.Tests;

/// <summary>
/// Integration tests for OpenAI Sora 2 API expansions (Mar 12, 2026).
/// Requires OPENAI_API_KEY environment variable.
/// </summary>
[TestFixture]
[Category("Integration")]
public class Sora2ApiIntegrationTests
{
    private TornadoApi _api = null!;

    [SetUp]
    public void Setup()
    {
        string? apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        if (string.IsNullOrEmpty(apiKey))
        {
            Assert.Ignore("OPENAI_API_KEY environment variable not set. Skipping Sora 2 integration tests.");
        }

        _api = new TornadoApi(LLmProviders.OpenAi, apiKey);
    }

    [Test]
    [Explicit("Requires API key and makes real Sora API calls")]
    public async Task CreateVideo_Sora2_4Seconds_Succeeds()
    {
        VideoGenerationRequest request = new VideoGenerationRequest(
            "Wide shot of a teal paper boat floating on a calm pond at dawn, soft morning light.",
            VideoModel.OpenAi.Sora.Sora2,
            duration: VideoDuration.Seconds4,
            aspectRatio: VideoAspectRatio.Widescreen,
            resolution: VideoResolution.HD
        );

        HttpCallResult<VideoJob> createResult = await _api.Videos.Create(request);
        Assert.That(createResult.Ok, Is.True, createResult.Exception?.Message ?? createResult.Response);
        Assert.That(createResult.Data, Is.Not.Null);
        Assert.That(createResult.Data!.Id, Is.Not.Empty);
        Assert.That(createResult.Data.Model, Does.Contain("sora"));
        Assert.That(createResult.Data.Seconds, Is.EqualTo("4"));
        Assert.That(createResult.Data.Size, Is.EqualTo("1280x720"));

        HttpCallResult<VideoJob> completed = await _api.Videos.WaitForCompletion(
            createResult.Data.Id,
            pollingIntervalMs: 15000,
            maxWaitMs: 900000
        );

        Assert.That(completed.Ok, Is.True, completed.Exception?.Message ?? completed.Response);
        Assert.That(completed.Data?.Status, Is.EqualTo(VideoJobStatus.Completed), completed.Data?.Error?.Message);

        StreamResponse? content = await _api.Videos.DownloadContent(completed.Data!);
        Assert.That(content, Is.Not.Null);
        Assert.That(content!.Stream.Length, Is.GreaterThan(0));
    }

    [Test]
    [Explicit("Requires API key and makes real Sora API calls")]
    public async Task CreateVideo_Sora2Pro_1080p_16Seconds_Succeeds()
    {
        VideoGenerationRequest request = new VideoGenerationRequest(
            "Slow dolly shot through a miniature paper city at blue hour, soft fog, practical window lights flickering on.",
            VideoModel.OpenAi.Sora.Sora2Pro,
            duration: VideoDuration.Seconds16,
            aspectRatio: VideoAspectRatio.Widescreen,
            resolution: VideoResolution.FullHD
        );

        HttpCallResult<VideoJob> createResult = await _api.Videos.Create(request);
        Assert.That(createResult.Ok, Is.True, createResult.Exception?.Message ?? createResult.Response);
        Assert.That(createResult.Data, Is.Not.Null);
        Assert.That(createResult.Data!.Seconds, Is.EqualTo("16"));
        Assert.That(createResult.Data.Size, Is.EqualTo("1920x1080"));

        HttpCallResult<VideoJob> completed = await _api.Videos.WaitForCompletion(
            createResult.Data.Id,
            pollingIntervalMs: 20000,
            maxWaitMs: 1800000
        );

        Assert.That(completed.Ok, Is.True, completed.Exception?.Message ?? completed.Response);
        Assert.That(completed.Data?.Status, Is.EqualTo(VideoJobStatus.Completed), completed.Data?.Error?.Message);
    }

    [Test]
    [Explicit("Requires API key and makes real Sora API calls")]
    public async Task CreateCharacter_AndUseInVideo_Succeeds()
    {
        string videoPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "Static", "video.mp4");
        if (!File.Exists(videoPath))
        {
            Assert.Ignore($"Test video not found at {videoPath}");
        }

        byte[] videoBytes = await File.ReadAllBytesAsync(videoPath);
        const string characterName = "TestMascot";

        HttpCallResult<VideoCharacter> characterResult = await _api.Videos.CreateCharacter(characterName, videoBytes);
        Assert.That(characterResult.Ok, Is.True, characterResult.Exception?.Message ?? characterResult.Response);
        Assert.That(characterResult.Data, Is.Not.Null);
        Assert.That(characterResult.Data!.Id, Is.Not.Empty);
        Assert.That(characterResult.Data.Name, Is.EqualTo(characterName));

        HttpCallResult<VideoCharacter> fetched = await _api.Videos.GetCharacter(characterResult.Data.Id);
        Assert.That(fetched.Ok, Is.True, fetched.Exception?.Message ?? fetched.Response);
        Assert.That(fetched.Data?.Id, Is.EqualTo(characterResult.Data.Id));

        VideoGenerationRequest request = new VideoGenerationRequest(
            $"A cinematic tracking shot of {characterName} walking through a lantern-lit market at dusk.",
            VideoModel.OpenAi.Sora.Sora2,
            duration: VideoDuration.Seconds4,
            aspectRatio: VideoAspectRatio.Widescreen,
            resolution: VideoResolution.HD
        )
        {
            OpenAiExtensions = new VideoOpenAiExtensions
            {
                Characters = [new VideoCharacterReference(characterResult.Data.Id)]
            }
        };

        HttpCallResult<VideoJob> createResult = await _api.Videos.Create(request);
        Assert.That(createResult.Ok, Is.True, createResult.Exception?.Message ?? createResult.Response);
        Assert.That(createResult.Data, Is.Not.Null);

        HttpCallResult<VideoJob> completed = await _api.Videos.WaitForCompletion(
            createResult.Data!.Id,
            pollingIntervalMs: 15000,
            maxWaitMs: 900000
        );

        Assert.That(completed.Ok, Is.True, completed.Exception?.Message ?? completed.Response);
        Assert.That(completed.Data?.Status, Is.EqualTo(VideoJobStatus.Completed), completed.Data?.Error?.Message);
    }

    [Test]
    [Explicit("Requires API key and makes real Sora API calls")]
    public async Task ExtendVideo_Succeeds()
    {
        VideoGenerationRequest baseRequest = new VideoGenerationRequest(
            "A red kite drifting over a grassy park at golden hour, camera slowly pans upward.",
            VideoModel.OpenAi.Sora.Sora2,
            duration: VideoDuration.Seconds4,
            aspectRatio: VideoAspectRatio.Widescreen,
            resolution: VideoResolution.HD
        );

        HttpCallResult<VideoJob> baseVideo = await _api.Videos.CreateAndWait(
            baseRequest,
            pollingIntervalMs: 15000,
            maxWaitMs: 900000
        );

        Assert.That(baseVideo.Ok, Is.True, baseVideo.Exception?.Message ?? baseVideo.Response);
        Assert.That(baseVideo.Data?.Status, Is.EqualTo(VideoJobStatus.Completed), baseVideo.Data?.Error?.Message);

        HttpCallResult<VideoJob> extendResult = await _api.Videos.Extend(new VideoExtensionRequest
        {
            VideoId = baseVideo.Data!.Id,
            Prompt = "Continue the scene as the camera rises over the treetops and reveals the sunset.",
            Duration = VideoDuration.Seconds4
        });

        Assert.That(extendResult.Ok, Is.True, extendResult.Exception?.Message ?? extendResult.Response);
        Assert.That(extendResult.Data, Is.Not.Null);

        HttpCallResult<VideoJob> completed = await _api.Videos.WaitForCompletion(
            extendResult.Data!.Id,
            pollingIntervalMs: 15000,
            maxWaitMs: 900000
        );

        Assert.That(completed.Ok, Is.True, completed.Exception?.Message ?? completed.Response);
        Assert.That(completed.Data?.Status, Is.EqualTo(VideoJobStatus.Completed), completed.Data?.Error?.Message);
    }

    [Test]
    [Explicit("Requires API key and makes real Sora API calls")]
    public async Task EditVideo_ById_Succeeds()
    {
        VideoGenerationRequest baseRequest = new VideoGenerationRequest(
            "Close-up of a steaming coffee cup on a wooden table, morning light through blinds.",
            VideoModel.OpenAi.Sora.Sora2,
            duration: VideoDuration.Seconds4,
            aspectRatio: VideoAspectRatio.Widescreen,
            resolution: VideoResolution.HD
        );

        HttpCallResult<VideoJob> baseVideo = await _api.Videos.CreateAndWait(
            baseRequest,
            pollingIntervalMs: 15000,
            maxWaitMs: 900000
        );

        Assert.That(baseVideo.Ok, Is.True, baseVideo.Exception?.Message ?? baseVideo.Response);
        Assert.That(baseVideo.Data?.Status, Is.EqualTo(VideoJobStatus.Completed), baseVideo.Data?.Error?.Message);

        HttpCallResult<VideoJob> editResult = await _api.Videos.Edit(new VideoEditRequest
        {
            VideoId = baseVideo.Data!.Id,
            Prompt = "Shift the color palette to teal, sand, and rust, with a warm backlight."
        });

        Assert.That(editResult.Ok, Is.True, editResult.Exception?.Message ?? editResult.Response);
        Assert.That(editResult.Data, Is.Not.Null);

        HttpCallResult<VideoJob> completed = await _api.Videos.WaitForCompletion(
            editResult.Data!.Id,
            pollingIntervalMs: 15000,
            maxWaitMs: 900000
        );

        Assert.That(completed.Ok, Is.True, completed.Exception?.Message ?? completed.Response);
        Assert.That(completed.Data?.Status, Is.EqualTo(VideoJobStatus.Completed), completed.Data?.Error?.Message);
    }

    [Test]
    [Explicit("Requires API key and makes real Sora API calls")]
    public async Task BatchVideo_CreateAccepted()
    {
        BatchRequest request = new BatchRequest([
            new BatchRequestItem("sora-shot-001", new VideoGenerationRequest(
                "Wide shot of paper lanterns floating over a quiet canal at night.",
                VideoModel.OpenAi.Sora.Sora2,
                duration: VideoDuration.Seconds4,
                aspectRatio: VideoAspectRatio.Widescreen,
                resolution: VideoResolution.HD
            ))
        ]);

        HttpCallResult<BatchItem> createResult = await _api.Batch.Create(request, LLmProviders.OpenAi);
        Assert.That(createResult.Ok, Is.True, createResult.Exception?.Message ?? createResult.Response);
        Assert.That(createResult.Data, Is.Not.Null);
        Assert.That(createResult.Data!.Id, Is.Not.Empty);
        Assert.That(createResult.Data.Endpoint, Is.EqualTo("/v1/videos"));
    }

    [Test]
    public void SerializeJson_Maps1080pAnd20Seconds()
    {
        VideoGenerationRequest request = new VideoGenerationRequest(
            "Test prompt",
            VideoModel.OpenAi.Sora.Sora2Pro,
            duration: VideoDuration.Seconds20,
            aspectRatio: VideoAspectRatio.Portrait,
            resolution: VideoResolution.FullHD
        );

        string json = Newtonsoft.Json.JsonConvert.SerializeObject(
            VendorOpenAiVideoRequest.SerializeJson(request)
        );

        Assert.That(json, Does.Contain("\"seconds\":\"20\""));
        Assert.That(json, Does.Contain("\"size\":\"1080x1920\""));
        Assert.That(json, Does.Contain("\"model\":\"sora-2-pro\""));
    }
}
