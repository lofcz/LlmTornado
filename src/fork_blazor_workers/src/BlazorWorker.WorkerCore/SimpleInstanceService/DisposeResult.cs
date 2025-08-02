using System;
using System.Collections.Generic;

namespace BlazorWorker.WorkerCore.SimpleInstanceService
{
    public class DisposeResult
    {
        public static readonly string Prefix = $"{SimpleInstanceService.MessagePrefix}{SimpleInstanceService.DiposeResultMessagePrefix}";

        public long CallId { get; set; }

        public bool IsSuccess { get; set; }
        public long InstanceId { get; set; }

        public string ExceptionMessage { get; set; } = string.Empty;

        public string FullExceptionString { get; set; } = string.Empty;

        public Exception Exception { get; internal set; }

        internal string Serialize()
        {
            return CSVSerializer.Serialize(Prefix,
               this.CallId,
               (this.IsSuccess ? 1 : 0),
               CSVSerializer.EscapeString(this.ExceptionMessage),
               CSVSerializer.EscapeString(this.FullExceptionString));
        }

        public static bool CanDeserialize(string message)
        {
            return message.StartsWith(Prefix);
        }

        public static DisposeResult Deserialize(string message)
        {
            var result = new DisposeResult();

            var parsers = new Queue<Action<string>>(
                new Action<string>[] {
                    s => result.CallId = long.Parse(s),
                    s => result.IsSuccess = s == "1",
                    s => result.ExceptionMessage = s,
                    s => result.FullExceptionString = s
            });

            CSVSerializer.Deserialize(Prefix, message, parsers);
            return result;
        }
    }
}