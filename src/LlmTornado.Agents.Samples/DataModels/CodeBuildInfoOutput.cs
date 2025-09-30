namespace LlmTornado.Agents.Samples.DataModels;

public struct CodeBuildInfoOutput
{
    public CodeBuildInfo BuildInfo { get; set; }
    public ProgramResultOutput ProgramResult { get; set; }

    public CodeBuildInfoOutput() { }
    public CodeBuildInfoOutput(CodeBuildInfo info, ProgramResultOutput codeResult)
    {
        BuildInfo = info;
        ProgramResult = codeResult;
    }
}

