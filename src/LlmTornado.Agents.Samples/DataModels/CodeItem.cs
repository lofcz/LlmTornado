namespace LlmTornado.Agents.Samples.DataModels;

public struct CodeItem
{
    public string filePath { get; set; }
    public string code { get; set; }

    public CodeItem(string path, string code)
    {
        filePath = path;
        this.code = code;
    }
}

