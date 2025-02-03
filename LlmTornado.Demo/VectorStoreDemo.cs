using LlmTornado.Common;
using LlmTornado.Files;
using LlmTornado.VectorStores;

namespace LlmTornado.Demo;

public static class VectorStoreDemo
{
    private static VectorStore? vectorStore;

    private static VectorStoreFile? vectorStoreFile;
    private static TornadoFile? file;
    private static VectorStoreFileBatch? vectorStoreFileBatch;

    private static string GenerateVectorStoreName() => $"demo_vector_store_{DateTime.Now.Ticks}";
    
    [TornadoTest]
    public static async Task<VectorStore> CreateVectorStore()
    {
        HttpCallResult<VectorStore> createResult = await Program.Connect().VectorStores.Create(
            new CreateVectorStoreRequest
            {
                Name = GenerateVectorStoreName()
            });
        vectorStore = createResult.Data;
        Console.WriteLine(createResult.Response);
        return createResult.Data!;
    }

    [TornadoTest]
    public static async Task<ListResponse<VectorStore>> ListVectorStores()
    {
        HttpCallResult<ListResponse<VectorStore>> listResult = await Program.Connect().VectorStores.List();
        Console.WriteLine(listResult.Response);
        return listResult.Data!;
    }

    [TornadoTest]
    public static async Task<VectorStore> RetrieveVectorStore()
    {
        if (vectorStore is null)
        {
            await CreateVectorStore();
        }

        HttpCallResult<VectorStore> retrieveResult =
            await Program.Connect().VectorStores.Retrieve(vectorStore!.Id);
        Console.WriteLine(retrieveResult.Response);
        return retrieveResult.Data!;
    }

    [TornadoTest]
    public static async Task<VectorStore> ModifyVectorStore()
    {
        if (vectorStore is null)
        {
            await CreateVectorStore();
        }

        HttpCallResult<VectorStore> modifyResult = await Program.Connect().VectorStores.Modify(
            vectorStore!.Id, new VectorStoreModifyRequest
            {
                Name = vectorStore.Name + "_modified"
            });
        Console.WriteLine(modifyResult.Response);
        return modifyResult.Data!;
    }

    [TornadoTest]
    public static async Task<VectorStoreFile> CreateVectorStoreFile()
    {
        if (vectorStore is null)
        {
            await CreateVectorStore();
        }

        file ??= await FilesDemo.Upload();

        HttpCallResult<VectorStoreFile> createResult = await Program.Connect().VectorStores.CreateFile(
            vectorStore!.Id, new CreateVectorStoreFileRequest
            {
                FileId = file!.Id
            });

        vectorStoreFile = createResult.Data!;
        Console.WriteLine(createResult.Response);
        return createResult.Data!;
    }
    
    [TornadoTest]
    public static async Task<VectorStoreFile> CreateVectorStoreFileCustomChunkingStrategy()
    {
        if (vectorStore is null)
        {
            await CreateVectorStore();
        }

        TornadoFile? file = await FilesDemo.Upload();
        if (file is null)
        {
            throw new Exception("could not upload file");
        }

        HttpCallResult<VectorStoreFile> createResult = await Program.Connect().VectorStores.CreateFile(
            vectorStore!.Id, new CreateVectorStoreFileRequest
            {
                FileId = file.Id,
                ChunkingStrategy = new StaticChunkingStrategy
                {
                    Static = new StaticChunkingConfig
                    {
                        MaxChunkSizeTokens = 500,
                        ChunkOverlapTokens = 100
                    }
                }
            });

        Console.WriteLine(createResult.Response);
        return createResult.Data!;
    }

    [TornadoTest]
    public static async Task<ListResponse<VectorStoreFile>> ListVectorStoreFiles()
    {
        if (vectorStore is null)
        {
            await CreateVectorStore();
        }

        HttpCallResult<ListResponse<VectorStoreFile>> listResult = await Program.Connect().VectorStores.ListFiles(vectorStore!.Id);
        Console.WriteLine(listResult.Response);
        return listResult.Data!;
    }

    [TornadoTest]
    public static async Task<VectorStoreFile> RetrieveVectorStoreFile()
    {
        if (vectorStore is null)
        {
            await CreateVectorStore();
        }

        if (vectorStoreFile is null)
        {
            await CreateVectorStoreFile();
        }

        HttpCallResult<VectorStoreFile> retrieveResult = await Program.Connect().VectorStores.RetrieveFiles(vectorStore!.Id, vectorStoreFile!.Id);
        Console.WriteLine(retrieveResult.Response);
        return retrieveResult.Data!;
    }

    [TornadoTest]
    public static async Task DeleteVectorStoreFile()
    {
        if (vectorStore is null)
        {
            await CreateVectorStore();
        }

        if (vectorStoreFile is null)
        {
            await CreateVectorStoreFile();
        }

        HttpCallResult<bool> deleteResult = await Program.Connect().VectorStores.DeleteFile(vectorStore!.Id, vectorStoreFile!.Id);

        Console.WriteLine(deleteResult.Response);
    }

    [TornadoTest]
    public static async Task<VectorStoreFileBatch> CreateVectorStoreFileBatch()
    {
        if (vectorStore is null)
        {
            await CreateVectorStore();
        }

        file ??= await FilesDemo.Upload();

        HttpCallResult<VectorStoreFileBatch> createResult = await Program.Connect().VectorStores
            .CreateBatchFile(
                vectorStore!.Id, new CreateVectorStoreFileBatchRequest
                {
                    FileIds = new List<string>
                    {
                        file!.Id
                    }
                });

        vectorStoreFileBatch = createResult.Data;
        Console.WriteLine(createResult.Response);
        return createResult.Data!;
    }

    [TornadoTest]
    public static async Task<ListResponse<VectorStoreFile>> ListVectorStoreBatchFiles()
    {
        if (vectorStore is null)
        {
            await CreateVectorStore();
        }

        if (vectorStoreFileBatch is null)
        {
            await CreateVectorStoreFileBatch();
        }

        HttpCallResult<ListResponse<VectorStoreFile>> listResult = await Program.Connect().VectorStores
            .ListBatchFiles(vectorStore!.Id, vectorStoreFileBatch!.Id);
        Console.WriteLine(listResult.Response);
        return listResult.Data!;
    }

    [TornadoTest]
    public static async Task<VectorStoreFileBatch> RetrieveVectorStoreFileBatch()
    {
        if (vectorStore is null)
        {
            await CreateVectorStore();
        }

        if (vectorStoreFileBatch is null)
        {
            await CreateVectorStoreFileBatch();
        }

        HttpCallResult<VectorStoreFileBatch> retrieveResult = await Program.Connect().VectorStores
            .RetrieveBatchFile(vectorStore!.Id, vectorStoreFileBatch!.Id);
        Console.WriteLine(retrieveResult.Response);
        return retrieveResult.Data!;
    }

    [TornadoTest]
    public static async Task CancelVectorStoreFileBatch()
    {
        if (vectorStore is null)
        {
            await CreateVectorStore();
        }

        if (vectorStoreFileBatch is null)
        {
            await CreateVectorStoreFileBatch();
        }

        HttpCallResult<VectorStoreFileBatch> cancelResult = await Program.Connect().VectorStores
            .CancelFileBatch(vectorStore!.Id, vectorStoreFileBatch!.Id);
        Console.WriteLine(cancelResult.Response);
    }

    [TornadoTest]
    public static async Task DeleteVectorStore()
    {
        if (vectorStore is null)
        {
            await CreateVectorStore();
        }

        HttpCallResult<bool> deleteResult = await Program.Connect().VectorStores.Delete(vectorStore!.Id);
        Console.WriteLine(deleteResult.Response);
    }
    
    [TornadoTest]
    public static async Task DeleteAllDemoVectorStores()
    {
        ListResponse<VectorStore> vectorStores = await ListVectorStores();
        List<Task<HttpCallResult<bool>>> tasks = (from vectorStore in vectorStores.Items
            where vectorStore.Name.StartsWith("demo_vector_store")
            select Program.Connect().VectorStores.Delete(vectorStore.Id)).ToList();
        Console.WriteLine($"Deleting {tasks.Count} vector stores...");
        await Task.WhenAll(tasks);
    }
}