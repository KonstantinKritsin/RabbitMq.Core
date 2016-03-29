using System;
using System.Collections.Generic;
using System.Text;
using Esb.RabbitMq.Core.Contracts;
using Newtonsoft.Json;
using RabbitMQ.Client;

namespace SubscriberNet35
{
    internal class QueueSerivceFactory : IQueueServiceFactory<ICMSService>
    {
        private readonly IConnection _connection;
        private readonly IModel _channel;
        private readonly Dictionary<string, QueueConsumer> _consumers = new Dictionary<string, QueueConsumer>();
        private readonly ICollection<IDisposable> _components = new List<IDisposable>();

        public QueueSerivceFactory()
        {
            var factory = new ConnectionFactory { HostName = "localhost", UserName = "guest", Password = "guest" };
            var type = typeof(ICMSService);

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();
            var methods = type.GetMethods();
            foreach (var methodInfo in methods)
            {
                var queueConsumer = new QueueConsumer
                {
                    ReplyQueueName = _channel.QueueDeclare().QueueName,
                    Consumer = new QueueingBasicConsumer(_channel)
                };
                _consumers.Add(methodInfo.Name, queueConsumer);
                _channel.BasicConsume(queueConsumer.ReplyQueueName, true, queueConsumer.Consumer);
            }
        }

        public ICMSService CreateService()
        {
            var service = new CmsService(this);
            _components.Add(service);
            return service;
        }

        private static TResult GetResponse<T, TResult>(T request, string rout, QueueSerivceFactory factory)
        {
            var queueConsumer = factory._consumers[rout];
            var corrId = Guid.NewGuid().ToString("N");
            var props = factory._channel.CreateBasicProperties();
            props.CorrelationId = corrId;
            props.ReplyTo = queueConsumer.ReplyQueueName;
#warning set expiration
            var json = JsonConvert.SerializeObject(request);
            factory._channel.BasicPublish(nameof(ICMSService), rout, props, Encoding.UTF8.GetBytes(json));

            while (true)
            {
                var ea = queueConsumer.Consumer.Queue.Dequeue();
                if (ea.BasicProperties.CorrelationId == corrId)
                {
                    var msg = Encoding.UTF8.GetString(ea.Body);
                    var obj = JsonConvert.DeserializeObject<TResult>(msg);
                    return obj;
                }
            }
        }

        private class QueueConsumer
        {
            public string ReplyQueueName { get; set; }
            public QueueingBasicConsumer Consumer { get; set; }
        }

        private class CmsService: ICMSService, IDisposable
        {
            private QueueSerivceFactory _factory;

            public CmsService(QueueSerivceFactory factory)
            {
                _factory = factory;
            }

            public BlockAgentInfo[] GetBCHAgents(AgencyFilter filter)
            {
                ThrowDisposed();
                return GetResponse<AgencyFilter, BlockAgentInfo[]>(filter, nameof(GetBCHAgents), _factory);
            }

            public PartnerData GetAgentDetailsByCode(string code)
            {
                ThrowDisposed();
                return GetResponse<string, PartnerData>(code, nameof(GetAgentDetailsByCode), _factory);
            }

            private void ThrowDisposed()
            {
                if(_factory == null)
                    throw new ObjectDisposedException(nameof(IQueueServiceFactory<ICMSService>));
            }

            public void Dispose()
            {
                _factory = null;
            }
        }

        public void Dispose()
        {
            foreach (var disposable in _components)
                disposable.Dispose();
            _channel?.Dispose();
            _connection?.Dispose();
        }
    }
}