using System;

namespace BlazorWorker.WorkerBackgroundService
{
    public class DisposeInstanceComplete : BaseMessage
    {
        public DisposeInstanceComplete()
        {
            MessageType = nameof(DisposeInstanceComplete);
        }

        public long CallId { get; set; }

        public bool IsSuccess { get; set; }

        public Exception Exception { get; set; }
    }
}
