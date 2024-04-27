using LlmTornado.Common;
using LlmTornado.Threads;

namespace LlmTornado.Demo;

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
    
    public static async Task<bool> Delete(ThreadResponse thread)
    {
        HttpCallResult<bool> deleted = await Program.Connect().Threads.DeleteThreadAsync(thread.Id);
        return deleted.Data;
    }
    
    public static async Task Delete()
    {
        ThreadResponse? response = await Create();
        HttpCallResult<bool> deleted = await Program.Connect().Threads.DeleteThreadAsync(response.Id);
        HttpCallResult<ThreadResponse> retrieveResponse = await Program.Connect().Threads.RetrieveThreadAsync(response.Id);
    }
    
    public static async Task CreateMessage()
    {
        ThreadResponse? response = await Create();
        HttpCallResult<MessageResponse> msg = await Program.Connect().Threads.CreateMessageAsync(response.Id, new CreateMessageRequest("my message"));
        await Delete(response);
    }
}