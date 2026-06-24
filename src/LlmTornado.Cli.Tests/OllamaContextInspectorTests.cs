using LlmTornado.Cli.Commands;

namespace LlmTornado.Cli.Tests;

[TestFixture]
public class OllamaContextInspectorTests
{
    [Test]
    public void ResolveHost_Normalizes_Common_Inputs()
    {
        Assert.That(OllamaContextInspector.ResolveHost(null), Is.EqualTo("http://localhost:11434"));
        Assert.That(OllamaContextInspector.ResolveHost("localhost:11434/"), Is.EqualTo("http://localhost:11434"));
        Assert.That(OllamaContextInspector.ResolveHost("http://0.0.0.0"), Is.EqualTo("http://127.0.0.1:11434"));
    }

    [Test]
    public void TryExtractContextTokens_Reads_ModelInfo_ContextLength()
    {
        string json = """
        {
          "model_info": {
            "llama.context_length": 131072
          },
          "parameters": "num_ctx 8192"
        }
        """;

        int? contextTokens = OllamaContextInspector.TryExtractContextTokens(json);
        Assert.That(contextTokens, Is.EqualTo(131072));
    }

    [Test]
    public void TryExtractContextTokens_Reads_Parameters_Object_And_String()
    {
        string objectJson = """
        {
          "parameters": {
            "num_ctx": "32768"
          }
        }
        """;

        string stringJson = """
        {
          "parameters": "repeat_penalty 1.1\nnum_ctx 65536\ntemperature 0.7"
        }
        """;

        Assert.That(OllamaContextInspector.TryExtractContextTokens(objectJson), Is.EqualTo(32768));
        Assert.That(OllamaContextInspector.TryExtractContextTokens(stringJson), Is.EqualTo(65536));
    }

    [Test]
    public void TryExtractRuntimeContextTokensFromPsJson_Reads_ModelSpecific_NumCtx()
    {
        string json = """
        {
          "models": [
            {
              "name": "other:latest",
              "details": { "num_ctx": 8192 }
            },
            {
              "name": "qwen3:14b",
              "details": { "num_ctx": 32768 }
            }
          ]
        }
        """;

        int? contextTokens = OllamaContextInspector.TryExtractRuntimeContextTokensFromPsJson(json, "qwen3:14b");
        Assert.That(contextTokens, Is.EqualTo(32768));
    }

    [Test]
    public void TryExtractRuntimeContextTokensFromPsText_Reads_Context_Column()
    {
        string psOutput = """
        NAME           ID              SIZE      PROCESSOR    CONTEXT    UNTIL
        qwen3:14b      abcdef123456    8.1 GB    100% GPU     3.2K/16K   4 minutes from now
        """;

        int? contextTokens = OllamaContextInspector.TryExtractRuntimeContextTokensFromPsText(psOutput, "qwen3:14b");
        Assert.That(contextTokens, Is.EqualTo(16000));
    }
}