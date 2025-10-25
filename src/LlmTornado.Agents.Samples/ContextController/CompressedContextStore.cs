using LlmTornado.Chat;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LlmTornado.Agents.Samples.ContextController;

public class CompressedContextStore
{
    public CompressedContextFile ContextFile { get; set; } = new CompressedContextFile();

    public MessageThread? CurrentThread => ContextFile.MessageThreads.LastOrDefault();

    public MessageThread? StartNewThread()
    {
        ContextFile.MessageThreads.Add(new MessageThread()
        {
            ThreadId = Guid.NewGuid().ToString(),
            CreateDate = DateTime.UtcNow
        });

        return CurrentThread;
    }

    public void AddTaskToCurrentThread(ContextTask task)
    {
        var thread = CurrentThread;
        if (thread == null)
        {
            thread = StartNewThread();
        }
        thread.Tasks.Add(task);
    }


    public void AddChunkToCurrentTask(ContextChunk chunk)
    {
        var thread = CurrentThread;
        if (thread == null)
        {
            thread = StartNewThread();
        }
        var task = thread.Tasks.LastOrDefault();
        if (task == null)
        {
            task = new ContextTask();
            thread.Tasks.Add(task);
        }
        task.Chunks.Add(chunk);
    }

    public void SerializeToFile(string filePath)
    {
        var json = JsonConvert.SerializeObject(ContextFile, Formatting.Indented);
        System.IO.File.WriteAllText(filePath, json);
    }

    public static CompressedContextStore DeserializeFromFile(string filePath)
    {
        var json = System.IO.File.ReadAllText(filePath);
        var contextFile = JsonConvert.DeserializeObject<CompressedContextFile>(json);
        return new CompressedContextStore { ContextFile = contextFile ?? new CompressedContextFile() };
    }
}

public class CompressedContextFile
{
    [JsonProperty("message_threads")]
    public List<MessageThread> MessageThreads { get; set; } = new List<MessageThread>();
}

public class MessageThread
{
    [JsonProperty("thread_id")]
    public string ThreadId { get; set; } = string.Empty;

    [JsonProperty("create_date")]
    public DateTime CreateDate { get; set; } = DateTime.UtcNow;

    [JsonProperty("tasks")]
    public List<ContextTask> Tasks { get; set; } = new List<ContextTask>();
}

public class ContextTask
{
    [JsonProperty("create_date")]
    public DateTime CreateDate { get; set; } = DateTime.UtcNow;

    [JsonProperty("instructions")] 
    public string Instructions { get; set; } = string.Empty;

    [JsonProperty("chunks")]
    public List<ContextChunk> Chunks { get; set; } = new List<ContextChunk>();
}

public class ContextChunk
{
    [JsonProperty("create_date")]
    public DateTime CreateDate { get; set; } = DateTime.UtcNow;

    [JsonProperty("content")]
    public string Content { get; set; } = string.Empty;

    [JsonProperty("message_ids")]
    public string[] MessageIds { get; set; } = Array.Empty<string>();
}
