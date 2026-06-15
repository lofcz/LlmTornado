using LlmTornado.Files;

namespace LlmTornado.Tests.Files;

[TestFixture]
public class OpenAiInputFileTypesTests
{
    [TestCase("sample.pdf", "application/pdf", OpenAiInputFileCategory.Pdf)]
    [TestCase("data.csv", "text/csv", OpenAiInputFileCategory.Spreadsheet)]
    [TestCase("report.docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document", OpenAiInputFileCategory.RichDocument)]
    [TestCase("slides.pptx", "application/vnd.openxmlformats-officedocument.presentationml.presentation", OpenAiInputFileCategory.Presentation)]
    [TestCase("main.py", "text/x-python", OpenAiInputFileCategory.TextAndCode)]
    [TestCase("readme.md", "text/markdown", OpenAiInputFileCategory.TextAndCode)]
    [TestCase("config.json", "application/json", OpenAiInputFileCategory.TextAndCode)]
    public void SupportedTypes_AreRecognized(string fileName, string mimeType, OpenAiInputFileCategory expectedCategory)
    {
        Assert.That(OpenAiInputFileTypes.IsSupportedExtension(fileName), Is.True);
        Assert.That(OpenAiInputFileTypes.IsSupportedMimeType(mimeType), Is.True);
        Assert.That(OpenAiInputFileTypes.TryValidate(fileName, mimeType, out string? error), Is.True, error);
        Assert.That(OpenAiInputFileTypes.TryGetCategory(fileName, out OpenAiInputFileCategory category), Is.True);
        Assert.That(category, Is.EqualTo(expectedCategory));
    }

    [TestCase("archive.zip")]
    [TestCase("image.png")]
    [TestCase("video.mp4")]
    public void UnsupportedExtensions_AreRejected(string fileName)
    {
        Assert.That(OpenAiInputFileTypes.IsSupportedExtension(fileName), Is.False);
        Assert.That(OpenAiInputFileTypes.TryValidate(fileName, null, out string? error), Is.False);
        Assert.That(error, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public void ValidateOrThrow_ThrowsForUnsupportedType()
    {
        Assert.Throws<ArgumentException>(() => OpenAiInputFileTypes.ValidateOrThrow("binary.exe"));
    }

    [Test]
    public void MimeTypes_ContainsFeb2026ExpansionEntries()
    {
        Assert.That(OpenAiInputFileTypes.MimeTypes, Does.Contain("text/x-python"));
        Assert.That(OpenAiInputFileTypes.MimeTypes, Does.Contain("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"));
        Assert.That(OpenAiInputFileTypes.Extensions, Does.Contain("pptx").And.Contain("xlsx").And.Contain("docx"));
    }
}
