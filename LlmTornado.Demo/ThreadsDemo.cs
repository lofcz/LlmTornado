using LlmTornado.Common;
using LlmTornado.Threads;
using Thread = LlmTornado.Threads.Thread;

namespace LlmTornado.Demo;

public static class ThreadsDemo
{
    private static Thread? generatedThread;

    public static async Task<Thread> Create()
    {
        HttpCallResult<Thread> thread = await Program.Connect().Threads.CreateThreadAsync();
        Console.WriteLine(thread.Response);
        generatedThread = thread.Data;
        return thread.Data!;
    }

    public static async Task<Thread> Retrieve()
    {
        generatedThread ??= await Create();
        HttpCallResult<Thread> response = await Program.Connect().Threads.RetrieveThreadAsync(generatedThread!.Id);
        Console.WriteLine(response.Response);
        return response.Data!;
    }

    public static async Task<Thread> Modify()
    {
        generatedThread ??= await Create();
        HttpCallResult<Thread>? response = await Program.Connect().Threads.ModifyThreadAsync(generatedThread.Id, new ModifyThreadRequest()
        {
            Metadata = new Dictionary<string, string>
            {
                {"key1", "value1"},
                {"key2", "value2"}
            }
        });
        Console.WriteLine(response.Response);
        return response.Data!;
    }

    public static async Task<bool> Delete(Thread thread)
    {
        HttpCallResult<bool> deleted = await Program.Connect().Threads.DeleteThreadAsync(thread.Id);
        return deleted.Data;
    }

    public static async Task Delete()
    {
        generatedThread ??= await Create();
        HttpCallResult<bool> deleted = await Program.Connect().Threads.DeleteThreadAsync(generatedThread.Id);
        Console.WriteLine(deleted.Response);
    }

    // public static async Task CreateMessage()
    // {
    //     Thread? response = await Create();
    //     HttpCallResult<MessageResponse> msg = await Program.Connect().Threads
    //         .CreateMessageAsync(response.Id, new CreateMessageRequest("my message"));
    //     await Delete(response);
    // }
}