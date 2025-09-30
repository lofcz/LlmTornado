namespace LlmTornado.Agents.Samples.DataModels;

public struct CodeReview
{
    public string ReviewSummary { get; set; }
    public CodeReviewItem[] Items { get; set; }

    public CodeReview(CodeReviewItem[] item)
    {
        Items = item;
    }
    public override string ToString()
    {
        return $""""
            From Code Review Summary:
            {ReviewSummary}

            Items to fix:

            {string.Join("\n\n", Items)}

            """";
    }
}

