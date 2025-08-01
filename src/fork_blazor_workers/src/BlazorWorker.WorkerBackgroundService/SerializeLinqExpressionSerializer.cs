using Serialize.Linq.Serializers;
using System.Linq.Expressions;

namespace BlazorWorker.WorkerBackgroundService
{
    public class SerializeLinqExpressionSerializer : IExpressionSerializer
    {
        private readonly ExpressionSerializer serializer;
        
        public SerializeLinqExpressionSerializer()
        {
            this.serializer = new ExpressionSerializer(new JsonSerializer());
        }

        public Expression Deserialize(string expressionString)
        {
            return serializer.DeserializeText(expressionString);
        }

        public string Serialize(Expression expression)
        {
            return serializer.SerializeText(expression);
        }
    }
}
