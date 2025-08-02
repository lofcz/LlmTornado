namespace BlazorWorker.Core.CoreInstanceService
{
    public class WorkerInstanceInitializeException : WorkerException
    {
        public WorkerInstanceInitializeException(string message, string fullMessage)
            :base($"Error when initializing instance: {message}", fullMessage)
        {
        }
    }
}
