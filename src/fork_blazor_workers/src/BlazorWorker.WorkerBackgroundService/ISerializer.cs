namespace BlazorWorker.WorkerBackgroundService
{
    public interface ISerializer
    {
        string Serialize(object obj);

        T Deserialize<T>(string objStr);
    }
}
