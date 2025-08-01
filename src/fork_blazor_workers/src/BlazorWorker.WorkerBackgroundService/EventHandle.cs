using System;
using System.Collections.Generic;
using System.Text;

namespace BlazorWorker.WorkerBackgroundService
{
    public class EventHandle
    {
        public delegate void HandlePayloadMessage(string payLoad);

        private static long idSource;
        public EventHandle()
        {
            Id = ++idSource;
        }
        public long Id { get; }

        public HandlePayloadMessage EventHandler { get; set; }
    }
}
