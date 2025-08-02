using BlazorWorker.WorkerCore;
using BlazorWorker.WorkerCore.SimpleInstanceService;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;
using System.Threading.Tasks;

namespace BlazorWorker.WorkerBackgroundService
{
    [SupportedOSPlatform("browser")]
    public partial class WorkerInstanceManager
    {
        private readonly ConcurrentDictionary<long, IEventWrapper> events =
             new();

        private static readonly MessageHandlerRegistry<WorkerInstanceManager> messageHandlerRegistry
            = new(wim => wim.serializer);

        public static readonly WorkerInstanceManager Instance = new();

        internal readonly ISerializer serializer;
        private readonly WebWorkerOptions options;
        
        private readonly MessageHandler<WorkerInstanceManager> messageHandler;
        private readonly SimpleInstanceService simpleInstanceService;

        static WorkerInstanceManager()
        {
            messageHandlerRegistry.Add<InitInstance>(wim => wim.InitInstance);
            messageHandlerRegistry.Add<InitInstanceFromFactory>(wim => wim.InitInstanceFromFactory);
            messageHandlerRegistry.Add<DisposeInstance>(wim => wim.DisposeInstance);
            messageHandlerRegistry.Add<MethodCallParams>(wim => wim.HandleMethodCall);
            messageHandlerRegistry.Add<RegisterEvent>(wim => wim.RegisterEvent);
            messageHandlerRegistry.Add<UnRegisterEvent>(wim => wim.UnRegisterEvent);
        }

        public WorkerInstanceManager()
        {
            this.serializer = new DefaultMessageSerializer();
            this.options = new WebWorkerOptions();
            var expressionSerializerType = Environment.GetEnvironmentVariable(WebWorkerOptions.ExpressionSerializerTypeEnvKey);
            if (expressionSerializerType != null)
            {
                this.options.ExpressionSerializerType = Type.GetType(expressionSerializerType);
            }
            
            this.simpleInstanceService = SimpleInstanceService.Instance;

            this.messageHandler = messageHandlerRegistry.GetRegistryForInstance(this);
        }

        [JSExport]
        public static void Init() {
            MessageService.Message += Instance.OnMessage;
            Instance.PostObject(new InitWorkerComplete());
#if DEBUG
            Console.WriteLine($"BlazorWorker.WorkerBackgroundService.{nameof(WorkerInstanceManager)}.Init(): Done.");
#endif
        }

        public void PostMessage(string message)
        {
#if DEBUG
            Console.WriteLine($"BlazorWorker.WorkerBackgroundService.{nameof(WorkerInstanceManager)}.PostMessage(): {message}.");
#endif
            MessageService.PostMessage(message);
        }

        internal void PostObject<T>(T obj)
        {
            PostMessage(this.serializer.Serialize(obj));
        }

        private bool IsInfrastructureMessage(string message)
        {
            return this.messageHandler.HandlesMessage(message);
        }

        private void OnMessage(object sender, string message)
        {
            this.messageHandler.HandleMessage(message);
        }

        private void HandleMethodCall(MethodCallParams methodCallMessage)
        {
            void handleError(Exception e)
            {
                PostObject(
                new MethodCallResult()
                {
                    CallId = methodCallMessage.CallId,
                    IsException = true,
                    ExceptionMessage = e.Message,
                    ExceptionString = e.ToString()
                });
            }

            try
            {
                Task.Run(async () =>
                    await MethodCall(methodCallMessage))
                    .ContinueWith(t => { 
                        if (t.IsFaulted)
                        {
                            handleError(t.Exception);
                        }
                        else
                        {
                            PostObject(
                                new MethodCallResult
                                {
                                    CallId = methodCallMessage.CallId,
                                    ResultPayload = this.serializer.Serialize(t.Result)
                                }
                            );
                        }
                    });
            }
            catch (Exception e)
            {
                handleError(e);
            }
        }

        private void UnRegisterEvent(UnRegisterEvent unregisterEventMessage)
        {
            if (!events.TryRemove(unregisterEventMessage.EventHandleId, out var wrapper)) {
                return;
            }

            wrapper.Unregister();
        }

        private void RegisterEvent(RegisterEvent registerEventMessage)
        {
            var instance = simpleInstanceService.instances[registerEventMessage.InstanceId].Instance;
            var instanceType = instance.GetType();
            var eventSignature = instanceType.GetEvent(registerEventMessage.EventName);
            if (eventSignature == null)
            {
                throw new ArgumentException($"{nameof(RegisterEvent)}: Unable to load event '{registerEventMessage.EventName}' for type '{instanceType.Name}'");
            }

            var eventType = Type.GetType(registerEventMessage.EventHandlerTypeArg);
            if (eventType == null)
            {
                throw new ArgumentException($"{nameof(RegisterEvent)}: Unable to load type '{registerEventMessage.EventHandlerTypeArg}' for event '{registerEventMessage.EventName}'");
            }
            
            var wrapperType = typeof(EventHandlerWrapper<>).MakeGenericType(eventType);
            
            var wrapper = (IEventWrapper)Activator.CreateInstance(wrapperType, this, registerEventMessage.InstanceId, registerEventMessage.EventHandleId);

            var delegateMethod = Delegate.CreateDelegate(eventSignature.EventHandlerType, wrapper, nameof(EventHandlerWrapper<object>.OnEvent)); 
            eventSignature.AddEventHandler(instance, delegateMethod);
            wrapper.Unregister = () => eventSignature.RemoveEventHandler(instance, delegateMethod);
            if (!events.TryAdd(wrapper.EventHandleId, wrapper))
            {
                throw new InvalidOperationException($"{nameof(WorkerInstanceManager)}.{nameof(RegisterEvent)}: Unable to register event with id {wrapper.EventHandleId}, id not available.");
            }
        }

        public void InitInstance(InitInstance createInstanceInfo)
        {
            var initResult = simpleInstanceService.InitInstance(
                new InitInstanceRequest
                {
                    Id = createInstanceInfo.InstanceId,
                    TypeName = createInstanceInfo.TypeName,
                    AssemblyName = createInstanceInfo.AssemblyName
                }, IsInfrastructureMessage);            
            
            PostObject(new InitInstanceComplete() { 
                CallId = createInstanceInfo.CallId, 
                IsSuccess = initResult.IsSuccess, 
                Exception = initResult.Exception,
            });
        }

        public void InitInstanceFromFactory(InitInstanceFromFactory createInstanceFromFactory)
        {
            try
            {
                if (!simpleInstanceService.instances.TryGetValue(createInstanceFromFactory.FactoryInstanceId, out var factory))
                {
                    throw new ArgumentException($"Unknown {nameof(createInstanceFromFactory.FactoryInstanceId)} {createInstanceFromFactory.FactoryInstanceId}");
                }

                var expression = this.options.ExpressionSerializer.Deserialize(createInstanceFromFactory.SerializedFactoryExpression);

                var child = (expression as LambdaExpression).Compile().DynamicInvoke(factory.Instance);
                var instanceWrapper = new InstanceWrapper { Instance = child };

                simpleInstanceService.instances[createInstanceFromFactory.InstanceId] = instanceWrapper;

                PostObject(new InitInstanceFromFactoryComplete
                {
                    CallId = createInstanceFromFactory.CallId,
                    IsSuccess = true
                });
            }
            catch (Exception e)
            {
                PostObject(new InitInstanceFromFactoryComplete
                {
                    CallId = createInstanceFromFactory.CallId,
                    IsSuccess = false,
                    Exception = e
                });
            }
        }

        public void DisposeInstance(DisposeInstance dispose)
        {
            var res = simpleInstanceService.DisposeInstance(
                new DisposeInstanceRequest { 
                    InstanceId = dispose.InstanceId,
                    CallId = dispose.CallId
                });

            PostObject(new DisposeInstanceComplete
            {
                CallId = res.CallId,
                IsSuccess = res.IsSuccess,
                Exception = res.Exception
            });
        }

        public async Task<object> MethodCall(MethodCallParams instanceMethodCallParams)
        {
            var instance = simpleInstanceService.instances[instanceMethodCallParams.InstanceId].Instance;
            var lambda = this.options.ExpressionSerializer.Deserialize(instanceMethodCallParams.SerializedExpression) 
                as LambdaExpression;
            var dynamicDelegate = lambda.Compile();
            var result = dynamicDelegate.DynamicInvoke(instance);
            
            if (!instanceMethodCallParams.AwaitResult)
            {
                return result;
            }
            
            var taskResult = result as Task;
            if (taskResult != null)
            {
                await taskResult;
            }
            else
            {
                throw new InvalidOperationException($"Unexpected return type. Expected {nameof(Task)}, found '{result.GetType()}'");
            }

            var resultType = taskResult.GetType();
            if (!resultType.IsGenericType)
            {
                // Task without result
                return null;
            }

            // Task<T>
            return resultType.GetProperty(nameof(Task<object>.Result)).GetValue(result);
        }
    }
}
