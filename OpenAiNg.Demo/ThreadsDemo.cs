using OpenAiNg.Common;
using OpenAiNg.Threads;

namespace OpenAiNg.Demo;

public static class ThreadsDemo
{
    public static async Task<ThreadResponse> Create()
    {
        HttpCallResult<ThreadResponse> thread = await Program.Connect().Threads.CreateThreadAsync();
        return thread.Data;
    }
}