namespace LlmTornado.Agents.Samples.DataModels;

public struct CodeReviewItem
{
    public string CodePath { get; set; }
    public string CodeError { get; set; }
    public string SuggestedFix { get; set; }
    public CodeReviewItem(string codePath, string codeError, string suggestedFix)
    {
        CodePath = codePath;
        CodeError = codeError;
        SuggestedFix = suggestedFix;
    }

    public override string ToString()
    {
        return $"""

             File: {CodePath}
             
             Had Error: 
             {CodeError}

             Suggested Fix:
             {SuggestedFix}

             """;
    }
}

