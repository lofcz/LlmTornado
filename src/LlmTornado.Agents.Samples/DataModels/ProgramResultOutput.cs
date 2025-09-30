namespace LlmTornado.Agents.Samples.DataModels;

public struct ProgramResultOutput
{
    public ProgramResult Result { get; set; }
    public string ProgramRequest { get; set; }
    public ProgramResultOutput(ProgramResult result, string request)
    {
        Result = result;
        ProgramRequest = request;
    }
}

