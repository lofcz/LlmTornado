using BlazorWorker.WorkerBackgroundService;
using Serialize.Linq.Serializers;
using System;
using System.Linq.Expressions;
using static BlazorWorker.Demo.SharedPages.Pages.ComplexSerialization;

namespace BlazorWorker.Demo.SharedPages.Shared
{
    /// <summary>
    /// Example 1: Simple custom expression Serializer using <see cref="Serialize.Linq.ExpressionSerializer"/> 
    /// as base class, but explicitly adds complex types as known types.
    /// </summary>
    public class CustomSerializeLinqExpressionJsonSerializer : SerializeLinqExpressionJsonSerializerBase
    {
        public override Type[] GetKnownTypes() => 
            [typeof(ComplexServiceArg), typeof(ComplexServiceResponse), typeof(OhLookARecord)];
    }

    /// <summary>
    /// Fully custom Expression Serializer, which uses <see cref="Serialize.Linq.ExpressionSerializer"/> but you could use an alternative implementation.
    /// </summary>
    public class CustomExpressionSerializer : IExpressionSerializer
    {
        private readonly ExpressionSerializer serializer;

        public CustomExpressionSerializer()
        {
            var specificSerializer = new JsonSerializer();
            specificSerializer.AddKnownType(typeof(ComplexServiceArg));
            specificSerializer.AddKnownType(typeof(ComplexServiceResponse));
            specificSerializer.AddKnownType(typeof(OhLookARecord));

            this.serializer = new ExpressionSerializer(specificSerializer);
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
