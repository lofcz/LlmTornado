using System;

namespace BlazorWorker.WorkerBackgroundService
{
    public class InitInstanceFromFactoryComplete : BaseMessage
    {
        public InitInstanceFromFactoryComplete()
        {
            MessageType = nameof(InitInstanceFromFactoryComplete);
        }

        public long CallId { get; set; }

        public bool IsSuccess { get; set; }

        public Exception Exception { get; set; }
    }
}
