namespace LlmTornado.FineTuning;

public enum JobStatus
{
    NotStarted = 0,
    ValidatingFiles,
    Queued,
    Running,
    Succeeded,
    Failed,
    Cancelled
}