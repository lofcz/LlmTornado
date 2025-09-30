namespace LlmTornado.Agents.Samples.DataModels;

public struct ReportData
{
    public string ShortSummary { get; set; }
    public string FinalReport { get; set; }
    public string[] FollowUpQuestions { get; set; }
    public ReportData(string shortSummary, string finalReport, string[] followUpQuestions)
    {
        this.ShortSummary = shortSummary;
        this.FinalReport = finalReport;
        this.FollowUpQuestions = followUpQuestions;
    }

    public override string ToString()
    {
        return $@"
Summary: 
{ShortSummary}

Final Report: 
{FinalReport}


Follow Up Questions: 
{string.Join("\n", FollowUpQuestions)}
";
    }
}
