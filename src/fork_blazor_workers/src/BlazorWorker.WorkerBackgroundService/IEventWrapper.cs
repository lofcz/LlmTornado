using System;

namespace BlazorWorker.WorkerBackgroundService
{
    public interface IEventWrapper
    {
        long InstanceId { get; }
        long EventHandleId { get; }
        Action Unregister { get; set; }
    }

}
