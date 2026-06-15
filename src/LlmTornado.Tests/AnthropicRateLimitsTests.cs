using LlmTornado.Common;
using LlmTornado.Code;
using LlmTornado.Code.Vendor;
using LlmTornado.Demo;
using LlmTornado.RateLimits;
using Newtonsoft.Json;

namespace LlmTornado.Tests;

/// <summary>
/// Unit and integration tests for the Anthropic Admin Rate Limits API.
/// </summary>
[TestFixture]
public class AnthropicRateLimitsTests
{
    private const string SampleOrgResponse = """
        {
          "data": [
            {
              "type": "rate_limit",
              "group_type": "model_group",
              "models": ["claude-opus-4-6", "claude-sonnet-4-6"],
              "limits": [
                { "type": "requests_per_minute", "value": 4000 },
                { "type": "input_tokens_per_minute", "value": 10000000 }
              ]
            },
            {
              "type": "rate_limit",
              "group_type": "batch",
              "models": null,
              "limits": [{ "type": "enqueued_batch_requests", "value": 500000 }]
            }
          ],
          "next_page": null
        }
        """;

    private const string SampleWorkspaceResponse = """
        {
          "data": [
            {
              "type": "workspace_rate_limit",
              "group_type": "model_group",
              "models": ["claude-opus-4-6"],
              "limits": [
                { "type": "requests_per_minute", "value": 1000, "org_limit": 4000 }
              ]
            }
          ],
          "next_page": null
        }
        """;

    [Test]
    public void Deserialize_OrganizationRateLimits_MatchesApiShape()
    {
        AnthropicRateLimitsListResponse? response = JsonConvert.DeserializeObject<AnthropicRateLimitsListResponse>(SampleOrgResponse);

        Assert.That(response, Is.Not.Null);
        Assert.That(response!.Data, Has.Count.EqualTo(2));
        Assert.That(response.NextPage, Is.Null);

        AnthropicRateLimitEntry modelGroup = response.Data[0];
        Assert.That(modelGroup.Type, Is.EqualTo("rate_limit"));
        Assert.That(modelGroup.GroupType, Is.EqualTo(AnthropicRateLimitGroupType.ModelGroup));
        Assert.That(modelGroup.Models, Has.Count.EqualTo(2));
        Assert.That(modelGroup.Limits[0].Type, Is.EqualTo("requests_per_minute"));
        Assert.That(modelGroup.Limits[0].Value, Is.EqualTo(4000));
        Assert.That(modelGroup.Limits[0].OrgLimit, Is.Null);

        AnthropicRateLimitEntry batch = response.Data[1];
        Assert.That(batch.GroupType, Is.EqualTo(AnthropicRateLimitGroupType.Batch));
        Assert.That(batch.Models, Is.Null);
    }

    [Test]
    public void Deserialize_WorkspaceRateLimits_IncludesOrgLimit()
    {
        AnthropicRateLimitsListResponse? response = JsonConvert.DeserializeObject<AnthropicRateLimitsListResponse>(SampleWorkspaceResponse);

        Assert.That(response, Is.Not.Null);
        Assert.That(response!.Data[0].Type, Is.EqualTo("workspace_rate_limit"));
        Assert.That(response.Data[0].Limits[0].OrgLimit, Is.EqualTo(4000));
        Assert.That(response.Data[0].Limits[0].Value, Is.EqualTo(1000));
    }

    [Test]
    public void ListQuery_ToQueryParams_IncludesOrgOnlyModelFilter()
    {
        AnthropicRateLimitsListQuery query = new AnthropicRateLimitsListQuery
        {
            GroupType = AnthropicRateLimitGroupType.Batch,
            Model = "claude-opus-4-8",
            Page = "page_abc"
        };

        Dictionary<string, object>? orgParams = query.ToQueryParams(includeModelFilter: true);
        Assert.That(orgParams, Is.Not.Null);
        Assert.That(orgParams!["group_type"], Is.EqualTo("batch"));
        Assert.That(orgParams["model"], Is.EqualTo("claude-opus-4-8"));
        Assert.That(orgParams["page"], Is.EqualTo("page_abc"));

        Dictionary<string, object>? workspaceParams = query.ToQueryParams(includeModelFilter: false);
        Assert.That(workspaceParams, Is.Not.Null);
        Assert.That(workspaceParams!.ContainsKey("model"), Is.False);
        Assert.That(workspaceParams["group_type"], Is.EqualTo("batch"));
    }

    [Test]
    public void AnthropicProvider_ApiUrl_BuildsOrganizationAndWorkspacePaths()
    {
        AnthropicEndpointProvider provider = new AnthropicEndpointProvider();

        string orgUrl = provider.ApiUrl(CapabilityEndpoints.RateLimits, null);
        string workspaceUrl = provider.ApiUrl(CapabilityEndpoints.RateLimits, "wrkspc_01Test");

        Assert.That(orgUrl, Is.EqualTo("https://api.anthropic.com/v1/organizations/rate_limits"));
        Assert.That(workspaceUrl, Is.EqualTo("https://api.anthropic.com/v1/organizations/workspaces/wrkspc_01Test/rate_limits"));
    }

    [Test]
    public void ListWorkspaceRateLimits_EmptyWorkspaceId_Throws()
    {
        TornadoApi api = new TornadoApi(LLmProviders.Anthropic, "sk-ant-admin-test");

        Assert.Throws<ArgumentException>(() =>
            api.RateLimits.ListWorkspaceRateLimits("  ").GetAwaiter().GetResult());
    }

    [Test]
    [Category("Integration")]
    [Explicit("Requires ANTHROPIC_ADMIN_API_KEY (sk-ant-admin...) and makes real Admin API calls")]
    public async Task ListOrganizationRateLimits_ReturnsData()
    {
        TornadoApi? api = ResolveAdminApi();
        if (api is null)
        {
            Assert.Ignore("ANTHROPIC_ADMIN_API_KEY not set (or apiKey.json AnthropicAdmin). Skipping integration test.");
        }

        HttpCallResult<AnthropicRateLimitsListResponse> result = await api!.RateLimits.ListOrganizationRateLimits();

        Assert.That(result.Ok, Is.True, () => result.Exception?.Message ?? result.Response ?? "Request failed");
        Assert.That(result.Data, Is.Not.Null);
        Assert.That(result.Data!.Data, Is.Not.Empty);

        AnthropicRateLimitEntry? modelGroup = result.Data.Data.FirstOrDefault(x => x.GroupType == AnthropicRateLimitGroupType.ModelGroup);
        Assert.That(modelGroup, Is.Not.Null);
        Assert.That(modelGroup!.Type, Is.EqualTo("rate_limit"));
        Assert.That(modelGroup.Limits, Is.Not.Empty);

        TestContext.WriteLine(JsonConvert.SerializeObject(result.Data, Formatting.Indented));
    }

    [Test]
    [Category("Integration")]
    [Explicit("Requires ANTHROPIC_ADMIN_API_KEY (sk-ant-admin...) and makes real Admin API calls")]
    public async Task ListOrganizationRateLimits_FilterByGroupType_Works()
    {
        TornadoApi? api = ResolveAdminApi();
        if (api is null)
        {
            Assert.Ignore("ANTHROPIC_ADMIN_API_KEY not set (or apiKey.json AnthropicAdmin). Skipping integration test.");
        }

        HttpCallResult<AnthropicRateLimitsListResponse> result = await api!.RateLimits.ListOrganizationRateLimits(
            new AnthropicRateLimitsListQuery { GroupType = AnthropicRateLimitGroupType.Batch });

        Assert.That(result.Ok, Is.True, () => result.Exception?.Message ?? result.Response ?? "Request failed");
        Assert.That(result.Data!.Data, Is.Not.Empty);
        Assert.That(result.Data.Data.All(x => x.GroupType == AnthropicRateLimitGroupType.Batch), Is.True);
    }

    [Test]
    [Category("Integration")]
    [Explicit("Requires ANTHROPIC_ADMIN_API_KEY, ANTHROPIC_WORKSPACE_ID, and makes real Admin API calls")]
    public async Task ListWorkspaceRateLimits_ReturnsOverridesOrEmpty()
    {
        TornadoApi? api = ResolveAdminApi();
        string? workspaceId = Environment.GetEnvironmentVariable("ANTHROPIC_WORKSPACE_ID");

        if (api is null)
        {
            Assert.Ignore("ANTHROPIC_ADMIN_API_KEY not set. Skipping integration test.");
        }

        if (string.IsNullOrWhiteSpace(workspaceId))
        {
            Assert.Ignore("ANTHROPIC_WORKSPACE_ID not set. Skipping workspace rate limits integration test.");
        }

        HttpCallResult<AnthropicRateLimitsListResponse> result = await api!.RateLimits.ListWorkspaceRateLimits(workspaceId!);

        Assert.That(result.Ok, Is.True, () => result.Exception?.Message ?? result.Response ?? "Request failed");
        Assert.That(result.Data, Is.Not.Null);

        foreach (AnthropicRateLimitEntry entry in result.Data!.Data)
        {
            Assert.That(entry.Type, Is.EqualTo("workspace_rate_limit"));
        }

        TestContext.WriteLine(JsonConvert.SerializeObject(result.Data, Formatting.Indented));
    }

    private static TornadoApi? ResolveAdminApi()
    {
        string? adminKey = Environment.GetEnvironmentVariable("ANTHROPIC_ADMIN_API_KEY");
        if (!string.IsNullOrWhiteSpace(adminKey))
        {
            return new TornadoApi(LLmProviders.Anthropic, adminKey);
        }

        if (Program.SetupApi().GetAwaiter().GetResult())
        {
            TornadoApi api = Program.Connect();
            string? key = api.GetProvider(LLmProviders.Anthropic).Auth?.ApiKey;
            if (!string.IsNullOrWhiteSpace(key) && key.StartsWith("sk-ant-admin", StringComparison.Ordinal))
            {
                return api;
            }
        }

        return null;
    }
}
