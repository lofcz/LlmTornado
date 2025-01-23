using LlmTornado.Common;
using LlmTornado.VectorStores;
using File = LlmTornado.Files.File;

namespace LlmTornado.Demo;

public static class VectorStoreDemo
{
    private static string? vectorStoreId = null;

    private static string? vectorStoreFileId = null;
    private static string? fileIdVectorStoreFile = null;

    private static string? vectorStoreFileBatchId = null;
    private static string? fileIdVectorStoreFileBatch = null;

    public static async Task<VectorStore> CreateVectorStore()
    {
        HttpCallResult<VectorStore> createResult = await Program.Connect().VectorStores.CreateVectorStoreAsync(
            new CreateVectorStoreRequest()
            {
                Name = "test_case_vector_can_be_deleted"
            });
        vectorStoreId = createResult.Data?.Id;
        Console.WriteLine(createResult.Response);
        return createResult.Data;
    }

    public static async Task<ListResponse<VectorStore>> ListVectorStores()
    {
        if (vectorStoreId == null)
        {
            throw new Exception("No vector store created");
        }

        HttpCallResult<ListResponse<VectorStore>> listResult =
            await Program.Connect().VectorStores.ListVectorStoresAsync();
        Console.WriteLine(listResult.Response);
        return listResult.Data;
    }

    public static async Task<VectorStore> RetrieveVectorStore()
    {
        if (vectorStoreId == null)
        {
            throw new Exception("No vector store created");
        }

        HttpCallResult<VectorStore> retrieveResult =
            await Program.Connect().VectorStores.RetrieveVectorStoreAsync(vectorStoreId);
        Console.WriteLine(retrieveResult.Response);
        return retrieveResult.Data;
    }

    public static async Task<VectorStore> ModifyVectorStore()
    {
        if (vectorStoreId == null)
        {
            throw new Exception("No vector store created");
        }

        HttpCallResult<VectorStore> modifyResult = await Program.Connect().VectorStores.ModifyVectorStoreAsync(
            vectorStoreId, new VectorStoreModifyRequest
            {
                Name = "test_case_vector_can_be_deleted_modified"
            });
        Console.WriteLine(modifyResult.Response);
        return modifyResult.Data;
    }

    public static async Task<VectorStoreFile> CreateVectorStoreFile()
    {
        if (vectorStoreId == null)
        {
            throw new Exception("No vector store created");
        }

        File? file = await FilesDemo.Upload();
        if (file == null)
        {
            throw new Exception("could not upload file");
        }

        fileIdVectorStoreFile = file.Id;

        HttpCallResult<VectorStoreFile> createResult = await Program.Connect().VectorStores.CreateVectorStoreFileAsync(
            vectorStoreId, new CreateVectorStoreFileRequest()
            {
                FileId = fileIdVectorStoreFile
            });

        vectorStoreFileId = createResult.Data?.Id;
        Console.WriteLine(createResult.Response);
        return createResult.Data;
    }

    public static async Task<ListResponse<VectorStoreFile>> ListVectorStoreFiles()
    {
        if (vectorStoreId == null)
        {
            throw new Exception("No vector store created");
        }

        HttpCallResult<ListResponse<VectorStoreFile>> listResult =
            await Program.Connect().VectorStores.ListVectorStoreFilesAsync(vectorStoreId);
        Console.WriteLine(listResult.Response);
        return listResult.Data;
    }

    public static async Task<VectorStoreFile> RetrieveVectorStoreFile()
    {
        if (vectorStoreId == null)
        {
            throw new Exception("No vector store created");
        }

        if (vectorStoreFileId == null)
        {
            throw new Exception("No vector store file created");
        }

        HttpCallResult<VectorStoreFile> retrieveResult = await Program.Connect().VectorStores
            .RetrieveVectorStoreFileAsync(vectorStoreId, vectorStoreFileId);
        Console.WriteLine(retrieveResult.Response);
        return retrieveResult.Data;
    }

    public static async Task DeleteVectorStoreFile()
    {
        if (vectorStoreId == null)
        {
            throw new Exception("No vector store created");
        }

        if (vectorStoreFileId == null)
        {
            throw new Exception("No vector store file created");
        }

        if (fileIdVectorStoreFile == null)
        {
            throw new Exception("No file uploaded");
        }

        HttpCallResult<bool> deleteResult = await Program.Connect().VectorStores
            .DeleteVectorStoreFileAsync(vectorStoreId, vectorStoreFileId);

        Console.WriteLine(deleteResult.Response);
    }

    public static async Task<VectorStoreFileBatch> CreateVectorStoreFileBatch()
    {
        if (vectorStoreId == null)
        {
            throw new Exception("No vector store created");
        }

        File? file = await FilesDemo.Upload();
        if (file == null)
        {
            throw new Exception("could not upload file");
        }

        fileIdVectorStoreFileBatch = file.Id;

        HttpCallResult<VectorStoreFileBatch> createResult = await Program.Connect().VectorStores
            .CreateVectorStoreFileBatchAsync(
                vectorStoreId, new CreateVectorStoreFileBatchRequest()
                {
                    FileIds = new List<string>()
                    {
                        fileIdVectorStoreFileBatch
                    }
                });

        vectorStoreFileBatchId = createResult.Data.Id;
        Console.WriteLine(createResult.Response);
        return createResult.Data;
    }

    public static async Task<ListResponse<VectorStoreFile>> ListVectorStoreBatchFiles()
    {
        if (vectorStoreId == null)
        {
            throw new Exception("No vector store created");
        }

        if (vectorStoreFileBatchId == null)
        {
            throw new Exception("No vector store file batch created");
        }

        HttpCallResult<ListResponse<VectorStoreFile>> listResult = await Program.Connect().VectorStores
            .ListVectorStoreBatchFilesAsync(vectorStoreId, vectorStoreFileBatchId);
        Console.WriteLine(listResult.Response);
        return listResult.Data;
    }

    public static async Task<VectorStoreFileBatch> RetrieveVectorStoreFileBatch()
    {
        if (vectorStoreId == null)
        {
            throw new Exception("No vector store created");
        }

        if (vectorStoreFileBatchId == null)
        {
            throw new Exception("No vector store file batch created");
        }

        HttpCallResult<VectorStoreFileBatch> retrieveResult = await Program.Connect().VectorStores
            .RetrieveVectorStoreFileBatchAsync(vectorStoreId, vectorStoreFileBatchId);
        Console.WriteLine(retrieveResult.Response);
        return retrieveResult.Data;
    }

    public static async Task CancelVectorStoreFileBatch()
    {
        if (vectorStoreId == null)
        {
            throw new Exception("No vector store created");
        }

        if (vectorStoreFileBatchId == null)
        {
            throw new Exception("No vector store file batch created");
        }

        HttpCallResult<VectorStoreFileBatch> cancelResult = await Program.Connect().VectorStores
            .CancelVectorStoreFileBatchAsync(vectorStoreId, vectorStoreFileBatchId);
        Console.WriteLine(cancelResult.Response);
    }


    public static async Task DeleteVectorStore()
    {
        if (vectorStoreId == null)
        {
            throw new Exception("No vector store created");
        }
        
        // final cleanup of uploaded files
        if (fileIdVectorStoreFile != null)
        {
            await FilesDemo.DeleteFile(fileIdVectorStoreFile);
        }

        if (fileIdVectorStoreFileBatch != null)
        {
            await FilesDemo.DeleteFile(fileIdVectorStoreFileBatch);
        }

        HttpCallResult<bool> deleteResult = await Program.Connect().VectorStores.DeleteVectorStoreAsync(vectorStoreId);
        Console.WriteLine(deleteResult.Response);
    }
}