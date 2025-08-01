namespace BlazorWorker.WorkerBackgroundService
{
    public class UnRegisterEvent : BaseMessage
    {
        public UnRegisterEvent()
        {
            MessageType = nameof(UnRegisterEvent);
        }

        public long EventHandleId { get; set;  }
    }
}
