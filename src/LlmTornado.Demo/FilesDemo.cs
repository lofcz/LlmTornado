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
    public static async Task<TornadoFile?> UploadAnthropic()
    {
        HttpCallResult<TornadoFile> uploadedFile = await Program.Connect().Files.Upload("Static/Files/sample.pdf", provider: LLmProviders.Anthropic);
        TornadoFile? retrievedFile = await Program.Connect().Files.Get(uploadedFile.Data?.Id, provider: LLmProviders.Anthropic);
        Console.WriteLine($"uploaded id: {uploadedFile.Data.Id}");
        Console.WriteLine($"retrieved file id: {retrievedFile?.Id}");
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
    public static async Task<TornadoPagingList<TornadoFile>?> GetAllFilesAnthropic()
    {
        TornadoPagingList<TornadoFile>? items = await Program.Connect().Files.Get(new ListQuery(100), provider: LLmProviders.Anthropic);

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
    public static async Task<TornadoPagingList<TornadoFile>?> DownloadAnthropic()
    {
        TornadoPagingList<TornadoFile>? items = await Program.Connect().Files.Get(new ListQuery(100), provider: LLmProviders.Anthropic);

        if (items?.Items.Count > 0)
        {
            TornadoFile? first = items.Items.FirstOrDefault(x => x.Downloadable);

            if (first is not null)
            {
                string content = await Program.Connect().Files.GetContent(first.Id, provider: LLmProviders.Anthropic);
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
        HttpCallResult<DeletedTornadoFile> deleteResult = await Program.Connect().Files.Delete(uploadedFile.Data.Id);
        Console.WriteLine($"{(deleteResult.Data?.Deleted ?? false ? "File deleted" : "File not deleted")}");
        return deleteResult is not null;
    }
    
    [TornadoTest]
    public static async Task<bool> DeleteFileGoogle()
    {
        HttpCallResult<TornadoFile> uploadedFile = await Program.Connect().Files.Upload("Static/Files/sample.pdf", provider: LLmProviders.Google);
        
        HttpCallResult<DeletedTornadoFile> deleteResult = await Program.Connect().Files.Delete(uploadedFile.Data.Id, provider: LLmProviders.Google);
        Console.WriteLine($"{(deleteResult.Data?.Deleted ?? false ? "File deleted" : "File not deleted")}");
        return deleteResult is not null;
    }
    
    [TornadoTest]
    public static async Task<bool> DeleteFileAnthropic()
    {
        HttpCallResult<TornadoFile> uploadedFile = await Program.Connect().Files.Upload("Static/Files/sample.pdf", provider: LLmProviders.Anthropic);
        HttpCallResult<DeletedTornadoFile> deleteResult = await Program.Connect().Files.Delete(uploadedFile.Data.Id, provider: LLmProviders.Anthropic);
        HttpCallResult<DeletedTornadoFile> deleteResult2 = await Program.Connect(false).Files.Delete(uploadedFile.Data.Id, provider: LLmProviders.Anthropic);
        Console.WriteLine($"{(deleteResult.Data?.Deleted ?? false ? $"File deleted - {deleteResult.Data.Id}" : "File not deleted")}");
        return deleteResult is not null;
    }
}