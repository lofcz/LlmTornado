using System;
using System.Collections.Generic;
using System.Text;

namespace BlazorWorker.WorkerCore.SimpleInstanceService
{
    public class InitInstanceRequest
    {

        public static readonly string Prefix = $"{SimpleInstanceService.MessagePrefix}{SimpleInstanceService.InitInstanceMessagePrefix}";
        public long CallId { get; set; }
        public long Id { get; set; }
        public string TypeName { get; set; }
        public string AssemblyName { get; set; }

        internal static bool CanDeserialize(string initMessage)
        {
            return initMessage.StartsWith(Prefix);
        }

        internal static InitInstanceRequest Deserialize(string initMessage)
        {
            var result = new InitInstanceRequest();

            var parsers = new Queue<Action<string>>(
                new Action<string>[] {
                    s => result.CallId = long.Parse(s),
                    s => result.Id = long.Parse(s),
                    s => result.TypeName = s,
                    s => result.AssemblyName = s
            });

            CSVSerializer.Deserialize(Prefix, initMessage, parsers);
            return result;
        }

        public string Serialize()
        {
            return CSVSerializer.Serialize(Prefix, 
                CallId, 
                Id, 
                CSVSerializer.EscapeString(TypeName), 
                CSVSerializer.EscapeString(AssemblyName));
        }
    }
}
