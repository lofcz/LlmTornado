namespace BlazorWorker.WorkerBackgroundService
{
    public class InitWorkerComplete : BaseMessage
    {
        public InitWorkerComplete()
        {
            MessageType = nameof(InitWorkerComplete);
        }
    }
}
