using LlmTornado.Assistants;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Common;
using LlmTornado.Threads;
using Thread = LlmTornado.Threads.Thread;

namespace LlmTornado.Demo;

public static class ThreadsDemo
{
    private static Thread? generatedThread;
    private static Message? generatedMessage;
    private static TornadoRun? generatedTornadoRun;

    [TornadoTest]
    public static async Task<Thread> CreateThread()
    {
        HttpCallResult<Thread> thread = await Program.Connect().Threads.CreateThreadAsync();
        Console.WriteLine(thread.Response);
        generatedThread = thread.Data;
        return thread.Data!;
    }

    [TornadoTest]
    public static async Task<Thread> RetrieveThread()
    {
        generatedThread ??= await CreateThread();
        HttpCallResult<Thread> response = await Program.Connect().Threads.RetrieveThreadAsync(generatedThread!.Id);
        Console.WriteLine(response.Response);
        return response.Data!;
    }

    [TornadoTest]
    public static async Task<Thread> ModifyThread()
    {
        generatedThread ??= await CreateThread();
        HttpCallResult<Thread>? response = await Program.Connect().Threads.ModifyThreadAsync(generatedThread.Id,
            new ModifyThreadRequest()
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

    [TornadoTest]
    public static async Task DeleteThread()
    {
        generatedThread ??= await CreateThread();
        HttpCallResult<bool> deleted = await Program.Connect().Threads.DeleteThreadAsync(generatedThread.Id);
        generatedThread = null;
        generatedMessage = null;
        generatedTornadoRun = null;
        Console.WriteLine(deleted.Response);
    }

    [TornadoTest]
    public static async Task<Message> CreateMessage()
    {
        generatedThread ??= await CreateThread();
        var response = await Program.Connect().Threads.CreateMessageAsync(generatedThread.Id, new CreateMessageRequest()
        {
            Role = ChatMessageRole.User,
            Content = "I need to think of a magic spell that turns cows into sheep."
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
                    {"key1", "value1"},
                    {"key2", "value2"}
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
        generatedMessage = null;
        Console.WriteLine(response.Response);
        return response.Data;
    }

    public static async Task<TornadoRun> CreateRun()
    {
        generatedMessage ??= await CreateMessage();
        Assistant? assistant = await AssistantsDemo.Create();

        HttpCallResult<TornadoRun> response = await Program.Connect().Threads.CreateRunAsync(generatedThread!.Id,
            new CreateRunRequest(assistant!.Id)
            {
                Model = ChatModel.OpenAi.Gpt4.O241120,
                Instructions = "You are a helpful assistant with the ability to create names of magic spells."
            });
        Console.WriteLine(response.Response);
        generatedTornadoRun = response.Data!;

        return response.Data!;
    }

    [TornadoTest]
    public static async Task<TornadoRun> RetrieveRun()
    {
        generatedTornadoRun ??= await CreateRun();

        HttpCallResult<TornadoRun> response =
            await Program.Connect().Threads.RetrieveRunAsync(generatedThread!.Id, generatedTornadoRun!.Id);
        Console.WriteLine(response.Response);
        return response.Data!;
    }

    [TornadoTest]
    public static async Task<TornadoRun> RetrieveRunAndPollForCompletion()
    {
        generatedTornadoRun ??= await CreateRun();

        while (true)
        {
            HttpCallResult<TornadoRun> response = await Program.Connect().Threads
                .RetrieveRunAsync(generatedThread!.Id, generatedTornadoRun!.Id);
            if (response.Data!.Status == RunStatus.Completed)
            {
                IReadOnlyList<Message> messages = await ListMessages();
                Console.WriteLine(response.Response);

                foreach (Message message in messages.Reverse())
                {
                    foreach (MessageContent content in message.Content)
                    {
                        if (content is MessageContentText text)
                        {
                            Console.WriteLine($"{message.Role}: {text.MessageContentTextData?.Value}");
                        }
                    }
                }

                return response.Data!;
            }

            await Task.Delay(TimeSpan.FromSeconds(1));
        }
    }

    public static async Task<TornadoRun> ModifyRun()
    {
        generatedTornadoRun ??= await CreateRun();
        
        HttpCallResult<TornadoRun> response = await Program.Connect().Threads.ModifyRunAsync(generatedThread!.Id, generatedTornadoRun!.Id,
            new ModifyRunRequest()
            {
                Metadata = new Dictionary<string, string>()
                {
                    {"key1", "value1"},
                    {"key2", "value2"}
                }
            });
        Console.WriteLine(response.Response);
        generatedTornadoRun = response.Data!;
        return response.Data!;
    }
    
    [TornadoTest]
    public static async Task<bool> DeleteRun()
    {
        generatedTornadoRun ??= await CreateRun();
        
        HttpCallResult<bool> response = await Program.Connect().Threads.DeleteRunAsync(generatedThread!.Id, generatedTornadoRun!.Id);
        Console.WriteLine(response.Response);
        generatedTornadoRun = null;
        return response.Data;
    }
    
    [TornadoTest]
    public static async Task<IReadOnlyList<TornadoRunStep>> ListRunSteps()
    {
        generatedTornadoRun ??= await RetrieveRunAndPollForCompletion();
        HttpCallResult<ListResponse<TornadoRunStep>> response = await Program.Connect().Threads.ListRunStepsAsync(generatedThread!.Id, generatedTornadoRun!.Id);
        Console.WriteLine(response.Response);
        return response.Data!.Items;
    }

    [TornadoTest]
    public static async Task<TornadoRunStep> RetrieveRunStep()
    {
        IReadOnlyList<TornadoRunStep> runSteps = await ListRunSteps();
        HttpCallResult<TornadoRunStep> response = await Program.Connect().Threads.RetrieveRunStepAsync(generatedThread!.Id, generatedTornadoRun!.Id, runSteps.FirstOrDefault()!.Id);
        Console.WriteLine(response.Response);
        return response.Data!;
    }
}