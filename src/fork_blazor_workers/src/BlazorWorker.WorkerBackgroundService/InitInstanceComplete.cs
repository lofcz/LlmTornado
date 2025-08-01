using System;

namespace BlazorWorker.WorkerBackgroundService
{
    public class InitInstanceComplete : BaseMessage
    {
        public InitInstanceComplete()
        {
            MessageType = nameof(InitInstanceComplete);
        }

        public long CallId { get; set; }

        public bool IsSuccess { get; set; }

        public Exception Exception { get; set; }
    }
}
