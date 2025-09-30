namespace LlmTornado.Agents.Samples.DataModels;

public struct FileOperationResult
{
    public string Operation { get; set; }
    public string FilePath { get; set; }
    public string Content { get; set; }
    public bool Success { get; set; }
    public string ErrorMessage { get; set; }
    public FileOperationResult(string operation, string filePath, string content, bool success, string errorMessage = "")
    {
        Operation = operation;
        FilePath = filePath;
        Content = content;
        Success = success;
        ErrorMessage = errorMessage;
    }
}

