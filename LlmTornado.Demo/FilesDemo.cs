using LlmTornado.Code;
using LlmTornado.Common;
using LlmTornado.Files;

namespace LlmTornado.Demo;

public class FilesDemo : DemoBase
{
    [TornadoTest]
    public static async Task<TornadoFile?> Upload()
    {
        TornadoFile? uploadedFile = await Upload("Static/Files/sample.pdf", FilePurpose.Assistants);
        TornadoFile? retrievedFile = await Program.Connect().Files.Get(uploadedFile?.Id);
        Console.WriteLine($"uploaded id: {uploadedFile.Id}");
        Console.WriteLine($"retrieved file id: {retrievedFile?.Id}");
        return uploadedFile;
    }
    
    public static async Task<TornadoFile?> Upload(string path, FilePurpose purpose)
    {
        HttpCallResult<TornadoFile> uploadedFile = await Program.Connect().Files.Upload(path, purpose);
        return uploadedFile.Data;
    }
    
    [TornadoTest]
    public static async Task<TornadoFile?> UploadGoogle()
    {
        HttpCallResult<TornadoFile> uploadedFile = await Program.Connect().Files.Upload("Static/Files/sample.pdf", provider: LLmProviders.Google);
        TornadoFile? retrievedFile = await Program.Connect().Files.Get(uploadedFile.Data?.Id, provider: LLmProviders.Google);
        Console.WriteLine($"uploaded id: {uploadedFile.Data.Id}");
        Console.WriteLine($"retrieved file id: {retrievedFile?.Id}");
        return uploadedFile.Data;
    }

    [TornadoTest]
    public static async Task<TornadoPagingList<TornadoFile>?> GetAllFilesGoogle()
    {
        TornadoPagingList<TornadoFile>? items = await Program.Connect().Files.Get(new ListQuery(100), provider: LLmProviders.Google);

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
    
    [TornadoTest]
    public static async Task<TornadoPagingList<TornadoFile>?> GetAllFilesOpenAi()
    {
        TornadoPagingList<TornadoFile>? items = await Program.Connect().Files.Get(provider: LLmProviders.OpenAi);

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
    
    [TornadoTest]
    public static async Task<bool> DeleteFileOpenAi()
    {
        HttpCallResult<TornadoFile> uploadedFile = await Program.Connect().Files.Upload("Static/Files/sample.pdf", FilePurpose.Assistants, provider: LLmProviders.OpenAi);
        DeletedTornadoFile? deleteResult = await Program.Connect().Files.Delete(uploadedFile.Data.Id);
        Console.WriteLine($"{(deleteResult?.Deleted ?? false ? "File delted" : "File not deleted")}");
        return deleteResult is not null;
    }
    
    [TornadoTest]
    public static async Task<bool> DeleteFileGoogle()
    {
        HttpCallResult<TornadoFile> uploadedFile = await Program.Connect().Files.Upload("Static/Files/sample.pdf", provider: LLmProviders.Google);
        
        DeletedTornadoFile? deleteResult = await Program.Connect().Files.Delete(uploadedFile.Data.Id, provider: LLmProviders.Google);
        Console.WriteLine($"{(deleteResult?.Deleted ?? false ? "File delted" : "File not deleted")}");
        return deleteResult is not null;
    }
}