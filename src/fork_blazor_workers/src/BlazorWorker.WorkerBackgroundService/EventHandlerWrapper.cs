using BlazorWorker.WorkerBackgroundService;
using System;

namespace BlazorWorker.WorkerBackgroundService
{
    public class EventHandlerWrapper<T> : IEventWrapper
    {
        private readonly WorkerInstanceManager wim;

        public EventHandlerWrapper(
            WorkerInstanceManager wim, 
            long instanceId, 
            long eventHandleId)
        {
            this.wim = wim;
            InstanceId = instanceId;
            EventHandleId = eventHandleId;
        }

        public long InstanceId { get; }
        public long EventHandleId { get; }

        public Action Unregister { get; set; }

        public void OnEvent(object _, T eventArgs)
        {
            wim.PostObject(new EventRaised()
            {
                EventHandleId = EventHandleId,
                InstanceId = InstanceId,
                ResultPayload = wim.serializer.Serialize(eventArgs)
            });
        }
    }

}
