namespace LlmTornado.Agents.Samples.DataModels;
public struct ProgramResult
{
    public CodeItem[] items { get; set; }
    public ProgramResult(CodeItem[] items)
    {
        this.items = items;
    }
}

