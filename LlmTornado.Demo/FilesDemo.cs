using LlmTornado.Code;
using LlmTornado.Common;
using LlmTornado.Files;

namespace LlmTornado.Demo;

public static class FilesDemo
{
    public static async Task<TornadoFile?> Upload()
    {
        HttpCallResult<TornadoFile> uploadedFile = await Program.Connect().Files.UploadFileAsync("Static/Files/sample.pdf", FilePurpose.Assistants);
        TornadoFile? retrievedFile = await Program.Connect().Files.GetFileAsync(uploadedFile.Data?.Id);
        Console.WriteLine($"uploaded id: {uploadedFile.Data.Id}");
        Console.WriteLine($"retrieved file id: {retrievedFile?.Id}");
        return uploadedFile.Data;
    }
    
    public static async Task<TornadoFile?> UploadGoogle()
    {
        HttpCallResult<TornadoFile> uploadedFile = await Program.Connect().Files.UploadFileAsync("Static/Files/sample.pdf", provider: LLmProviders.Google);
        TornadoFile? retrievedFile = await Program.Connect().Files.GetFileAsync(uploadedFile.Data?.Id, provider: LLmProviders.Google);
        Console.WriteLine($"uploaded id: {uploadedFile.Data.Id}");
        Console.WriteLine($"retrieved file id: {retrievedFile?.Id}");
        return uploadedFile.Data;
    }

    public static async Task<TornadoPagingList<TornadoFile>?> GetAllFilesGoogle()
    {
        TornadoPagingList<TornadoFile>? items = await Program.Connect().Files.GetFilesAsync(new ListQuery(100), provider: LLmProviders.Google);

        if (items is not null)
        {
            Console.WriteLine($"Found {items.Items.Count} files.");
            
            foreach (TornadoFile item in items.Items)
            {
                Console.WriteLine(item.Id);
            }
        }

        return items;
    }
    
    public static async Task<TornadoPagingList<TornadoFile>?> GetAllFilesOpenAi()
    {
        TornadoPagingList<TornadoFile>? items = await Program.Connect().Files.GetFilesAsync(provider: LLmProviders.OpenAi);

        if (items is not null)
        {
            Console.WriteLine($"Found {items.Items.Count} files.");
            
            foreach (TornadoFile item in items.Items)
            {
                Console.WriteLine(item.Id);
            }
        }

        return items;
    }
    
    public static async Task<bool> DeleteFile(string fileId)
    {
        TornadoFile? deleteResult = await Program.Connect().Files.DeleteFileAsync(fileId);
        return deleteResult is not null;
    }
}