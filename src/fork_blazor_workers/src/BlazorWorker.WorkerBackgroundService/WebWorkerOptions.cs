using System;

namespace BlazorWorker.WorkerBackgroundService
{
    public class WebWorkerOptions
    {
        /// <summary>
        /// Name of environment variable to be used for transferring the serializer typename
        /// </summary>
        public static readonly string ExpressionSerializerTypeEnvKey = "BLAZORWORKER_EXPRESSIONSERIALIZER";

        private ISerializer messageSerializer;
        private IExpressionSerializer expressionSerializer;
        private Type expressionSerializerType;



        public ISerializer MessageSerializer { 
            get => messageSerializer ?? (messageSerializer = new DefaultMessageSerializer()); 
            set => messageSerializer = value; 
        }

        public IExpressionSerializer ExpressionSerializer {
            get => expressionSerializer ?? (expressionSerializer = CreateSerializerInstance()); 
            set => expressionSerializer = value; 
        }

        public Type ExpressionSerializerType
        {
            get => expressionSerializerType ?? typeof(SerializeLinqExpressionSerializer);
            set => expressionSerializerType = ValidateExpressionSerializerType(value);
        }

        /// <summary>
        /// Ensures that the provided type implements <see cref="IExpressionSerializer"/>
        /// </summary>
        /// <param name="sourceType"></param>
        /// <returns></returns>
        private Type ValidateExpressionSerializerType(Type sourceType)
        {
            if (sourceType == null)
            {
                return null;
            }

            if (!sourceType.IsClass)
            {
                throw new Exception($"The {nameof(ExpressionSerializerType)} '{sourceType.AssemblyQualifiedName}' must be a class.");
            }

            if (!typeof(IExpressionSerializer).IsAssignableFrom(sourceType))
            {
                throw new Exception($"The {nameof(ExpressionSerializerType)} '{sourceType.AssemblyQualifiedName}' must be assignable to {nameof(IExpressionSerializer)}");
            }

            return sourceType;
        }

        private IExpressionSerializer CreateSerializerInstance()
        {
            var instance = Activator.CreateInstance(ExpressionSerializerType);
            return (IExpressionSerializer)instance;
        }
    }
}
