using System.Linq.Expressions;

namespace BlazorWorker.WorkerBackgroundService
{
    public interface IExpressionSerializer
    {
        string Serialize(Expression expr);

        Expression Deserialize(string exprString);
    }
}
