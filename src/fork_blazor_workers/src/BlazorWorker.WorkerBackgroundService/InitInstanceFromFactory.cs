namespace BlazorWorker.WorkerBackgroundService
{
    public class InitInstanceFromFactory : BaseMessage
    {
        public InitInstanceFromFactory()
        {
            MessageType = nameof(InitInstanceFromFactory);
        }

        public long WorkerId { get; set; }
        public long InstanceId { get; set; }

        public long FactoryInstanceId { get; set; }
        public string SerializedFactoryExpression { get; set; }

        public long CallId { get; set; }
    }
}
