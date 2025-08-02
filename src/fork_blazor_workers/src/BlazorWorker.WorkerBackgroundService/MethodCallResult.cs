namespace BlazorWorker.WorkerBackgroundService
{
    public class MethodCallResult : BaseMessage
    {
        public MethodCallResult()
        {
            MessageType = nameof(MethodCallResult);
        }

        public string ResultPayload { get; set; }

        public bool IsException { get; set; }

        public string ExceptionString { get; set; }

        public string ExceptionMessage { get; set; }

        public long CallId { get; set; }
    }
}
