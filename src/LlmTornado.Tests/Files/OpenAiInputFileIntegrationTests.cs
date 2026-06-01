using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Code;
using LlmTornado.Common;
using LlmTornado.Demo;
using LlmTornado.Files;
using LlmTornado.Responses;

namespace LlmTornado.Tests.Files;

/// <summary>
/// Production integration tests for OpenAI expanded <c>input_file</c> support (Feb 24, 2026).
/// </summary>
[TestFixture]
[Category("Integration")]
public class OpenAiInputFileIntegrationTests
{
    private TornadoApi? _api;
    private static string StaticFilesDir => Path.Combine(TestContext.CurrentContext.TestDirectory, "Static", "Files");

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        string? envKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        if (!string.IsNullOrWhiteSpace(envKey))
        {
            _api = new TornadoApi(LLmProviders.OpenAi, envKey);
            return;
        }

        if (await Program.SetupApi())
        {
            _api = Program.Connect();
        }
    }

    private static IEnumerable<TestCaseData> InputFileCases()
    {
        yield return new TestCaseData("sample.pdf", "application/pdf", "What is this document? Reply in one short sentence.")
            .SetName("Pdf");
        yield return new TestCaseData("a11.txt", "text/plain", "What number appears in this file? Reply with only the number.")
            .SetName("Txt");
        yield return new TestCaseData("golden_data.json", "application/json", "Does this JSON contain a \"users\" key? Reply yes or no only.")
            .SetName("Json");
        yield return new TestCaseData("sample.csv", "text/csv", "How many data rows (excluding header)? Reply with only the number.")
            .SetName("Csv");
        yield return new TestCaseData(
                Path.Combine("Skills", "codebase-context-extractor", "context_extractor.py"),
                "text/x-python",
                "What language is this file? Reply with one word.")
            .SetName("Python");
    }

    [Test]
    [TestCaseSource(nameof(InputFileCases))]
    [Explicit("Requires OpenAI API key and makes real production API calls")]
    public async Task Responses_UploadedFile_ReturnsAnswer(string relativePath, string mimeType, string prompt)
    {
        if (_api is null)
        {
            Assert.Ignore("OpenAI API key not configured. Set OPENAI_API_KEY or provide apiKey.json.");
        }

        string filePath = Path.Combine(StaticFilesDir, relativePath);
        Assert.That(File.Exists(filePath), Is.True, $"Missing test file: {filePath}");

        Assert.That(OpenAiInputFileTypes.TryValidate(Path.GetFileName(filePath), mimeType, out _), Is.True);

        HttpCallResult<TornadoFile> upload = await _api.Files.Upload(
            filePath,
            FilePurpose.UserData,
            mimeType: mimeType,
            provider: LLmProviders.OpenAi);

        Assert.That(upload.Ok, Is.True, () => upload.Exception?.Message ?? "Upload failed");
        Assert.That(upload.Data?.Id, Is.Not.Null.And.Not.Empty);

        ResponseResult response = await _api.Responses.CreateResponse(new ResponseRequest
        {
            Model = ChatModel.OpenAi.Gpt5.V5Mini,
            InputItems =
            [
                new ResponseInputMessage(ChatMessageRoles.User,
                [
                    new ResponseInputContentText(prompt),
                    ResponseInputContentFile.CreateFromFile(upload.Data!, validate: false)
                ])
            ]
        });

        Assert.That(response.Output, Is.Not.Null.And.Not.Empty);
        string? text = response.OutputText;
        Assert.That(text, Is.Not.Null.And.Not.Empty);
        TestContext.WriteLine($"{relativePath} => {text}");
    }

    [Test]
    [Explicit("Requires OpenAI API key and makes real production API calls")]
    public async Task ChatCompletions_UploadedPdf_ReturnsAnswer()
    {
        if (_api is null)
        {
            Assert.Ignore("OpenAI API key not configured. Set OPENAI_API_KEY or provide apiKey.json.");
        }

        string filePath = Path.Combine(StaticFilesDir, "sample.pdf");
        HttpCallResult<TornadoFile> upload = await _api.Files.Upload(
            filePath,
            FilePurpose.UserData,
            mimeType: "application/pdf",
            provider: LLmProviders.OpenAi);

        Assert.That(upload.Ok, Is.True);

        HttpCallResult<ChatResult> chat = await _api.Chat.CreateChatCompletionSafe(new ChatRequest
        {
            Model = ChatModel.OpenAi.Gpt5.V5Mini,
            Messages =
            [
                new ChatMessage(ChatMessageRoles.User, [
                    new ChatMessagePart("Summarize this PDF in one sentence."),
                    new ChatMessagePart(new ChatMessagePartFileLinkData(upload.Data!))
                ])
            ]
        });

        Assert.That(chat.Ok, Is.True, () => chat.Exception?.Message ?? "Chat failed");
        Assert.That(chat.Data?.Choices?[0].Message?.Content, Is.Not.Null.And.Not.Empty);
        TestContext.WriteLine($"Chat PDF => {chat.Data!.Choices![0].Message!.Content}");
    }

    [Test]
    public void Upload_UnsupportedType_ReturnsBadRequestWithoutCallingApi()
    {
        TornadoApi api = new TornadoApi(LLmProviders.OpenAi, "sk-test");
        byte[] bytes = [0x50, 0x4B, 0x03, 0x04];

        HttpCallResult<TornadoFile> result = api.Files.Upload(
            bytes,
            "payload.zip",
            FilePurpose.UserData,
            mimeType: "application/zip",
            provider: LLmProviders.OpenAi).Result;

        Assert.That(result.Ok, Is.False);
        Assert.That(result.Code, Is.EqualTo(System.Net.HttpStatusCode.BadRequest));
    }
}
