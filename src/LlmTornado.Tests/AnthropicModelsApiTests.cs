using LlmTornado.Code;
using LlmTornado.Code.Vendor;
using LlmTornado.Demo;
using LlmTornado.Models;
using LlmTornado.Models.Vendors;
using LlmTornado.Models.Vendors.Anthropic;

namespace LlmTornado.Tests;

[TestFixture]
public class AnthropicModelsApiTests
{
    private const string SampleModelJson = """
        {
          "id": "claude-opus-4-6",
          "capabilities": {
            "batch": {
              "supported": true
            },
            "citations": {
              "supported": true
            },
            "code_execution": {
              "supported": true
            },
            "context_management": {
              "clear_thinking_20251015": {
                "supported": true
              },
              "clear_tool_uses_20250919": {
                "supported": true
              },
              "compact_20260112": {
                "supported": true
              },
              "supported": true
            },
            "effort": {
              "high": {
                "supported": true
              },
              "low": {
                "supported": true
              },
              "max": {
                "supported": true
              },
              "medium": {
                "supported": true
              },
              "supported": true,
              "xhigh": {
                "supported": true
              }
            },
            "image_input": {
              "supported": true
            },
            "pdf_input": {
              "supported": true
            },
            "structured_outputs": {
              "supported": true
            },
            "thinking": {
              "supported": true,
              "types": {
                "adaptive": {
                  "supported": true
                },
                "enabled": {
                  "supported": true
                }
              }
            }
          },
          "created_at": "2026-02-04T00:00:00Z",
          "display_name": "Claude Opus 4.6",
          "max_input_tokens": 200000,
          "max_tokens": 64000,
          "type": "model"
        }
        """;

    private const string SampleListJson = """
        {
          "data": [
            {
              "id": "claude-opus-4-6",
              "capabilities": {
                "batch": {
                  "supported": true
                },
                "thinking": {
                  "supported": true,
                  "types": {
                    "adaptive": {
                      "supported": true
                    },
                    "enabled": {
                      "supported": true
                    }
                  }
                }
              },
              "created_at": "2026-02-04T00:00:00Z",
              "display_name": "Claude Opus 4.6",
              "max_input_tokens": 200000,
              "max_tokens": 64000,
              "type": "model"
            }
          ],
          "first_id": "claude-opus-4-6",
          "has_more": false,
          "last_id": "claude-opus-4-6"
        }
        """;

    [Test]
    public void DeserializeModel_ParsesAnthropicCapabilityFields()
    {
        RetrievedModel? model = VendorAnthropicRetrievedModelsDeserializer.DeserializeModel(SampleModelJson, null);

        Assert.That(model, Is.Not.Null);
        Assert.That(model!.Id, Is.EqualTo("claude-opus-4-6"));
        Assert.That(model.Name, Is.EqualTo("Claude Opus 4.6"));
        Assert.That(model.Type, Is.EqualTo("model"));
        Assert.That(model.MaxInputTokens, Is.EqualTo(200000));
        Assert.That(model.MaxTokens, Is.EqualTo(64000));
        Assert.That(model.Created, Is.EqualTo(new DateTime(2026, 2, 4, 0, 0, 0, DateTimeKind.Utc)));
        Assert.That(model.Capabilities, Is.Not.Null);
        Assert.That(model.Capabilities!.Batch?.Supported, Is.True);
        Assert.That(model.Capabilities.ContextManagement?.Supported, Is.True);
        Assert.That(model.Capabilities.ContextManagement?.Compact20260112?.Supported, Is.True);
        Assert.That(model.Capabilities.Effort?.XHigh?.Supported, Is.True);
        Assert.That(model.Capabilities.Thinking?.Types?.Adaptive?.Supported, Is.True);
        Assert.That(model.Capabilities.StructuredOutputs?.Supported, Is.True);
    }

    [Test]
    public void DeserializeList_ParsesAnthropicModelsResult()
    {
        RetrievedModelsResult? result = RetrievedModelsResult.Deserialize(LLmProviders.Anthropic, SampleListJson, null);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Data, Has.Count.EqualTo(1));
        Assert.That(result.Data[0].Capabilities?.Batch?.Supported, Is.True);
        Assert.That(result.Data[0].MaxInputTokens, Is.EqualTo(200000));
    }

    [Test]
    public void AnthropicEndpointProvider_InboundMessage_ParsesRetrievedModel()
    {
        AnthropicEndpointProvider provider = new AnthropicEndpointProvider();
        RetrievedModel? model = provider.InboundMessage<RetrievedModel>(SampleModelJson, null, null);

        Assert.That(model?.Capabilities?.Thinking?.Supported, Is.True);
        Assert.That(model?.MaxTokens, Is.EqualTo(64000));
    }
}

[TestFixture]
[Category("Integration")]
public class AnthropicModelsApiIntegrationTests
{
    private TornadoApi? _api;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        string? envKey = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");
        if (!string.IsNullOrWhiteSpace(envKey))
        {
            _api = new TornadoApi(LLmProviders.Anthropic, envKey);
            return;
        }

        if (await Program.SetupApi() && !string.IsNullOrWhiteSpace(Program.ApiKeys.Anthropic))
        {
            _api = new TornadoApi(LLmProviders.Anthropic, Program.ApiKeys.Anthropic);
        }
    }

    [Test]
    [Explicit("Requires Anthropic API key and makes real production API calls")]
    public async Task ListModels_ReturnsCapabilityFields()
    {
        if (_api is null)
        {
            Assert.Ignore("Anthropic API key not configured. Set ANTHROPIC_API_KEY or provide apiKey.json.");
        }

        List<RetrievedModel>? models = await _api!.Models.GetModels(LLmProviders.Anthropic);

        Assert.That(models, Is.Not.Null);
        Assert.That(models, Is.Not.Empty);

        RetrievedModel model = models!.First(m => m.Capabilities is not null);

        Assert.That(model.Id, Is.Not.Null.And.Not.Empty);
        Assert.That(model.Capabilities, Is.Not.Null);
        Assert.That(model.Capabilities!.Batch, Is.Not.Null);
        Assert.That(model.MaxInputTokens, Is.Not.Null);
        Assert.That(model.MaxTokens, Is.Not.Null);
        Assert.That(model.Created, Is.Not.Null);
    }

    [Test]
    [Explicit("Requires Anthropic API key and makes real production API calls")]
    public async Task GetModelDetails_ReturnsCapabilityFields()
    {
        if (_api is null)
        {
            Assert.Ignore("Anthropic API key not configured. Set ANTHROPIC_API_KEY or provide apiKey.json.");
        }

        List<RetrievedModel>? models = await _api!.Models.GetModels(LLmProviders.Anthropic);
        Assert.That(models, Is.Not.Null);
        Assert.That(models, Is.Not.Empty);

        string modelId = models!.First(m => m.Capabilities is not null).Id;
        RetrievedModel? model = await _api.Models.GetRetrievedModelDetails(modelId, LLmProviders.Anthropic);

        Assert.That(model, Is.Not.Null);
        Assert.That(model!.Id, Is.EqualTo(modelId));
        Assert.That(model.Capabilities, Is.Not.Null);
        Assert.That(model.MaxInputTokens, Is.Not.Null);
        Assert.That(model.MaxTokens, Is.Not.Null);
    }
}
