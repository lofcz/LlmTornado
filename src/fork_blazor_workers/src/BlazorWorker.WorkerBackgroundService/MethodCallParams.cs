namespace BlazorWorker.WorkerBackgroundService
{
    public class MethodCallParams : BaseMessage
    {
        public MethodCallParams()
        {
            MessageType = nameof(MethodCallParams);
        }

        public bool AwaitResult { get; set; }
        public long InstanceId { get; set; }
        public string SerializedExpression { get; set; }
        public long WorkerId { get; set; }
        public long CallId { get; set; }
    }
}
