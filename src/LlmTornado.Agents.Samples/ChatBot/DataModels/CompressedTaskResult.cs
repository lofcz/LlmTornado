using System.ComponentModel;

namespace ChatBot.DataModels;

[Description("A compressed representation of a task and its result.")]
public struct CompressedTaskResult
{
    public string Task { get; set; }
    public string Result { get; set; }
}
