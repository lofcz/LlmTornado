using System;
using System.Collections.Generic;
using System.Data;
using System.Runtime.Serialization;

namespace BlazorWorker.WorkerBackgroundService
{
    public class MessageHandlerRegistry<THandler> : Dictionary<string, Action<THandler, string>>
    {
        public const string UnknownMessageType = "__unknownMessage";

        public MessageHandlerRegistry(Func<THandler, ISerializer> messageSerializer)
        {
            MessageSerializer = messageSerializer;
        }

        public Func<THandler, ISerializer> MessageSerializer { get; }

        public void Add<T>(Func<THandler, Action<T>> messageHandler) where T : BaseMessage
        {
            base.Add(typeof(T).Name,
                (handlerInstance, message) =>
                    DeserializeAndExecuteHandler(messageHandler, handlerInstance, message));
        }

        private void DeserializeAndExecuteHandler<TMessage>(Func<THandler, Action<TMessage>> messageHandler, THandler handlerInstance, string message) where TMessage : BaseMessage
        {
            TMessage typedMessage;
            try
            {
                typedMessage = MessageSerializer(handlerInstance).Deserialize<TMessage>(message);
            }
            catch (Exception e)
            {
                throw new SerializationException($"Unable to deserialize string '{message}' to message type {typeof(TMessage)}", e);
            }
            
            messageHandler(handlerInstance)(typedMessage);
        }

        public bool HandleMessage(THandler handlerInstance, string message)
        {
            if (!base.TryGetValue(GetMessageType(handlerInstance, message), out var handler))
            {
                return false;
            }

            handler(handlerInstance, message);
            return true;
        }

        public bool HandlesMessage(THandler handlerInstance, string message)
        {
            var key = GetMessageType(handlerInstance, message);
            return base.ContainsKey(key);
        }

        internal string GetMessageType(THandler handlerInstance, string message)
        {
            try
            {
                return this.MessageSerializer(handlerInstance).Deserialize<BaseMessage>(message).MessageType;
            }
            catch (Exception)
            {
                return UnknownMessageType;
            }
            
        }

        public MessageHandler<THandler> GetRegistryForInstance(THandler instance)
        {
            return new MessageHandler<THandler>(this, instance);
        }
    }

    public class MessageHandler<THandler>
    {
        private readonly MessageHandlerRegistry<THandler> registry;
        private readonly THandler instance;

        public MessageHandler(MessageHandlerRegistry<THandler> registry, THandler instance)
        {
            this.registry = registry;
            this.instance = instance;
        }

        public bool HandleMessage(string message)
        {
            return this.registry.HandleMessage(instance, message);
        }

        public bool HandlesMessage(string message)
        {
            return this.registry.HandlesMessage(instance, message);
        }
    }
}

