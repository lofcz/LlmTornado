using LlmTornado.Chat;
using LlmTornado.Common;
using LlmTornado.Threads;
using Thread = LlmTornado.Threads.Thread;

namespace LlmTornado.Demo;

public static class ThreadsDemo
{
    private static Thread? generatedThread;
    private static Message? generatedMessage;

    [TornadoTest]
    public static async Task<Thread> CreateThread()
    {
        HttpCallResult<Thread> thread = await Program.Connect().Threads.CreateThreadAsync();
        Console.WriteLine(thread.Response);
        generatedThread = thread.Data;
        return thread.Data!;
    }

    [TornadoTest]
    public static async Task<Thread> Retrieve()
    {
        generatedThread ??= await CreateThread();
        HttpCallResult<Thread> response = await Program.Connect().Threads.RetrieveThreadAsync(generatedThread!.Id);
        Console.WriteLine(response.Response);
        return response.Data!;
    }

    [TornadoTest]
    public static async Task<Thread> Modify()
    {
        generatedThread ??= await CreateThread();
        HttpCallResult<Thread>? response = await Program.Connect().Threads.ModifyThreadAsync(generatedThread.Id,
            new ModifyThreadRequest()
            {
                Metadata = new Dictionary<string, string>
                {
                    { "key1", "value1" },
                    { "key2", "value2" }
                }
            });
        Console.WriteLine(response.Response);
        return response.Data!;
    }

    [TornadoTest]
    public static async Task<bool> Delete(Thread thread)
    {
        HttpCallResult<bool> deleted = await Program.Connect().Threads.DeleteThreadAsync(thread.Id);
        return deleted.Data;
    }

    public static async Task Delete()
    {
        generatedThread ??= await CreateThread();
        HttpCallResult<bool> deleted = await Program.Connect().Threads.DeleteThreadAsync(generatedThread.Id);
        if (deleted.Data)
        {
            generatedThread = null;
        }

        Console.WriteLine(deleted.Response);
    }

    [TornadoTest]
    public static async Task<Message> CreateMessage()
    {
        generatedThread ??= await CreateThread();
        var response = await Program.Connect().Threads.CreateMessageAsync(generatedThread.Id, new CreateMessageRequest()
        {
            Role = ChatMessageRole.User,
            Content = "Hey, how are you?"
        });
        Console.WriteLine(response.Response);
        generatedMessage = response.Data!;
        return response.Data!;
    }

    [TornadoTest]
    public static async Task<Message> RetrieveMessage()
    {
        generatedThread ??= await CreateThread();
        generatedMessage ??= await CreateMessage();

        HttpCallResult<Message> response =
            await Program.Connect().Threads.RetrieveMessageAsync(generatedThread.Id, generatedMessage.Id);
        Console.WriteLine(response.Response);
        return response.Data!;
    }

    [TornadoTest]
    public static async Task<IReadOnlyList<Message>> ListMessages()
    {
        generatedThread ??= await CreateThread();
        generatedMessage ??= await CreateMessage();

        HttpCallResult<ListResponse<Message>> response =
            await Program.Connect().Threads.ListMessagesAsync(generatedThread.Id);
        Console.WriteLine(response.Response);
        return response.Data!.Items;
    }

    [TornadoTest]
    public static async Task<Message> ModifyMessage()
    {
        generatedThread ??= await CreateThread();
        generatedMessage ??= await CreateMessage();

        HttpCallResult<Message> response = await Program.Connect().Threads.ModifyMessageAsync(generatedThread.Id,
            generatedMessage.Id, new ModifyMessageRequest()
            {
                Metadata = new Dictionary<string, string>()
                {
                    { "key1", "value1" },
                    { "key2", "value2" }
                }
            });

        Console.WriteLine(response.Response);
        return response.Data!;
    }

    [TornadoTest]
    public static async Task<bool> DeleteMessage()
    {
        generatedThread ??= await CreateThread();
        generatedMessage ??= await CreateMessage();

        HttpCallResult<bool> response =
            await Program.Connect().Threads.DeleteMessageAsync(generatedThread.Id, generatedMessage.Id);
        Console.WriteLine(response.Response);
        return response.Data;
    }
}