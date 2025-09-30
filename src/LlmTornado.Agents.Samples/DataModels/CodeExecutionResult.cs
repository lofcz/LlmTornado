namespace LlmTornado.Agents.Samples.DataModels;

public struct CodeExecutionResult
{
    public string Code { get; set; }
    public string Language { get; set; }
    public string Output { get; set; }
    public bool Success { get; set; }
    public string ErrorMessage { get; set; }
    public CodeExecutionResult(string code, string language, string output, bool success, string errorMessage = "")
    {
        Code = code;
        Language = language;
        Output = output;
        Success = success;
        ErrorMessage = errorMessage;
    }
}

