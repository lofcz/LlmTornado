using Serialize.Linq.Serializers;
using System;
using System.Linq.Expressions;

namespace BlazorWorker.WorkerBackgroundService
{
    /// <summary>
    /// Base class for adding known types to a serializer using <see cref="Serialize.Linq.Serializers.JsonSerializer"/>.
    /// </summary>
    public abstract class SerializeLinqExpressionJsonSerializerBase : IExpressionSerializer
    {
        private ExpressionSerializer serializer;

        private ExpressionSerializer Serializer
            => this.serializer ?? (this.serializer = GetSerializer());

        /// <summary>
        /// Automatically adds known types as array types. If set to <c>true</c>, sets <see cref="AutoAddKnownTypesAsListTypes"/> to <c>false</c>.
        /// </summary>
        public bool? AutoAddKnownTypesAsArrayTypes { get; set; }

        /// <summary>
        /// Automatically adds known types as list types. If set to <c>true</c>, sets <see cref="AutoAddKnownTypesAsArrayTypes"/> to <c>false</c>.
        /// </summary>
        public bool? AutoAddKnownTypesAsListTypes { get; set; }

        public abstract Type[] GetKnownTypes();

        private ExpressionSerializer GetSerializer()
        {
            var jsonSerializer = new JsonSerializer();
            foreach (var type in GetKnownTypes())
            {
                jsonSerializer.AddKnownType(type);
            }
            if (this.AutoAddKnownTypesAsArrayTypes.HasValue) {
                jsonSerializer.AutoAddKnownTypesAsArrayTypes = this.AutoAddKnownTypesAsArrayTypes.Value; 
            }
            if (this.AutoAddKnownTypesAsListTypes.HasValue)
            {
                jsonSerializer.AutoAddKnownTypesAsListTypes = this.AutoAddKnownTypesAsListTypes.Value;
            }

            return new ExpressionSerializer(jsonSerializer);
        }

        public Expression Deserialize(string expressionString)
        {
            return this.Serializer.DeserializeText(expressionString);
        }

        public string Serialize(Expression expression)
        {
            return this.Serializer.SerializeText(expression);
        }
    }
}
