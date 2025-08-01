using System.Threading.Tasks;

namespace BlazorWorker.WorkerCore.SimpleInstanceService
{
    public interface ISimpleInstanceService
    {
        Task<DisposeResult> DisposeInstance(DisposeInstanceRequest request);
        Task<InitInstanceResult> InitInstance(InitInstanceRequest initInstanceRequest);
    }
}