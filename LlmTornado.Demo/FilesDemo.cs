using LlmTornado.Common;
using LlmTornado.Files;
using File = LlmTornado.Files.File;

namespace LlmTornado.Demo;

public static class FilesDemo
{
    public static async Task<File?> Upload()
    {
        HttpCallResult<File> uploadedFile = await Program.Connect().Files.UploadFileAsync("Static/Files/sample.pdf", FilePurpose.Assistants);
        File? retrievedFile = await Program.Connect().Files.GetFileAsync(uploadedFile.Data?.Id);
        return uploadedFile.Data;
    }
}