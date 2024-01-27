using OpenAiNg.Files;
using File = OpenAiNg.Files.File;

namespace OpenAiNg.Demo;

public class FilesDemo
{
    public static async Task<File?> Upload()
    {
        File? uploadedFile = await Program.Connect().Files.UploadFileAsync("Static/Files/sample.pdf", FilePurpose.Assistants);
        File? retrievedFile = await Program.Connect().Files.GetFileAsync(uploadedFile.Id);
        return uploadedFile;
    }
}