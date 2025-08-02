using System;
using System.Text;
namespace BlazorWorker.BackgroundServiceFactory
{

    public class BackgroundServiceWorkerException: Exception
    {
        private readonly string workerExceptionString;
        private string _stacktrace;

        public long WorkerId { get; }

        public BackgroundServiceWorkerException(long workerId, string message, string workerExceptionString) : base(message)
        {
            this.WorkerId = workerId;
            this.workerExceptionString = workerExceptionString;
        }

        public override string StackTrace => (this._stacktrace ??= BuildStackTrace());

        private string BuildStackTrace()
        {
            var stack = new StringBuilder();
            
            stack.AppendLine(this.workerExceptionString);
            stack.AppendLine($"   --- Worker Process Border (WorkerId: {this.WorkerId}) ---");
            stack.AppendLine(base.StackTrace);

            return stack.ToString();
        }
    }
    
}
