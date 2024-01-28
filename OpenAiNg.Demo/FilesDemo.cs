using OpenAiNg.Common;
using OpenAiNg.Files;
using File = OpenAiNg.Files.File;

namespace OpenAiNg.Demo;

public static class FilesDemo
{
    public static async Task<File?> Upload()
    {
        HttpCallResult<File> uploadedFile = await Program.Connect().Files.UploadFileAsync("Static/Files/sample.pdf", FilePurpose.Assistants);
        File? retrievedFile = await Program.Connect().Files.GetFileAsync(uploadedFile.Data?.Id);
        return uploadedFile.Data;
    }
}