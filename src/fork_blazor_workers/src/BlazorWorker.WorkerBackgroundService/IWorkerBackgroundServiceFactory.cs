using BlazorWorker.Core;
using System.Threading.Tasks;

namespace BlazorWorker.WorkerBackgroundService
{
    public interface IWorkerBackgroundServiceFactory
    {
        Task<IWorker> CreateWebworkerAsync();
    }
}
