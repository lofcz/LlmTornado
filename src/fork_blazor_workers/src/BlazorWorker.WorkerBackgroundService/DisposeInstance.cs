namespace BlazorWorker.WorkerBackgroundService
{
    public class DisposeInstance : BaseMessage
    {
        public DisposeInstance()
        {
            MessageType = nameof(DisposeInstance);
        }

        public long InstanceId { get; set; }

        public long CallId { get; set; }
    }
}
