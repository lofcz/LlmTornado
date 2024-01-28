using OpenAiNg.Common;
using OpenAiNg.Threads;

namespace OpenAiNg.Demo;

public static class ThreadsDemo
{
    public static async Task<ThreadResponse?> Create()
    {
        HttpCallResult<ThreadResponse> thread = await Program.Connect().Threads.CreateThreadAsync();
        return thread.Data;
    }

    public static async Task<ThreadResponse> Retrieve()
    {
        ThreadResponse? response = await Create();
        response = (await Program.Connect().Threads.RetrieveThreadAsync(response.Id)).Data;
        return response;
    }
    
    public static async Task<ThreadResponse> Modify()
    {
        ThreadResponse? response = await Create();
        response = (await Program.Connect().Threads.ModifyThreadAsync(response.Id, new Dictionary<string, string>
        {
            { "key1", "value1" },
            { "key2", "value2" }
        })).Data;
        response = (await Program.Connect().Threads.RetrieveThreadAsync(response.Id)).Data;
        return response;
    }
    
    public static async Task Delete()
    {
        ThreadResponse? response = await Create();
        HttpCallResult<bool> deleted = await Program.Connect().Threads.DeleteThreadAsync(response.Id);
        HttpCallResult<ThreadResponse> retrieveResponse = await Program.Connect().Threads.RetrieveThreadAsync(response.Id);
    }
}