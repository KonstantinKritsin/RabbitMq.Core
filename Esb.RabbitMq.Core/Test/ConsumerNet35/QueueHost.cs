using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Esb.RabbitMq.Core;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace ConsumerNet35
{
    public class QueueHost<T> : IDisposable where T : class
    {
        static QueueHost()
        {
            if(!typeof(T).IsInterface)
                throw new Exception(nameof(QueueHost<T>) + " interface generic parameter required");
        }

        private readonly IConnection _connection;
        private readonly IModel _channel;
        private readonly Dictionary<string, ConsumerInfo> _consumers = new Dictionary<string, ConsumerInfo>();
        private readonly T _service;

        public QueueHost(T service)
        {
            _service = service;
            var factory = new ConnectionFactory { HostName = "localhost", UserName = "guest", Password = "guest" };
            var type = typeof(T);

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();
            _channel.BasicQos(0, 10, false);
            _channel.ExchangeDeclare(type.Name, "direct");

            var methods = type.GetMethods();
            foreach (var methodInfo in methods)
            {
                // очередь без обращения живет сутки
                // {"x-message-ttl", 60000}
                var args = new Dictionary<string, object> { { "x-expires", 86400000 } };
                var queue = _channel.QueueDeclare(methodInfo.Name, false, false, false, args);
                if (queue != null)
                {
                    _channel.QueueBind(queue.QueueName, type.Name, methodInfo.Name);
                    var consumer = new EventingBasicConsumer(_channel);
                    consumer.Received += ConsumerOnReceived;
                    _consumers.Add(queue.QueueName, new ConsumerInfo {Consumer = consumer});
                    _channel.BasicConsume(queue.QueueName, false, consumer);
                }
            }
        }

        private void ConsumerOnReceived(IBasicConsumer sender, BasicDeliverEventArgs ea)
        {
            var props = ea.BasicProperties;
            var replyProps = _channel.CreateBasicProperties();
            replyProps.CorrelationId = props.CorrelationId;
            Console.WriteLine($"exchange: {ea.Exchange}; rout: {ea.RoutingKey}; correlationId: {props.CorrelationId}; replyTo: {props.ReplyTo}");
            string response = null;
            try
            {
                var message = Encoding.UTF8.GetString(ea.Body);
                var service = _service as ExperimentalImplementation;
                if (service == null)
                    return;

                response = CallService(ea.RoutingKey, message);

            }
            catch (Exception ex)
            {
                response = JsonConvert.SerializeObject(new ErrorMessage
                {
                    T = ex.GetType().AssemblyQualifiedName,
                    E = ex
                });
                Console.WriteLine(ex.Message);
            }
            finally
            {
                if (!string.IsNullOrEmpty(props.ReplyTo))
                    _channel.BasicPublish("", props.ReplyTo, replyProps, Encoding.UTF8.GetBytes(response));
                _channel.BasicAck(ea.DeliveryTag, false);
            }
        }

        private string CallService(string methodName, string message)
        {
            var consumerInfo = _consumers[methodName];
            if (consumerInfo.Method == null)
            {
                // соответствие однозначное, поэтому First
                var method = typeof (T).GetMethods().First(m => m.Name == methodName);
                var parameterInfos = method.GetParameters().ToDictionary(p => p.Name);
                Type resultType = null;
                PropertyInfo resultProp = null;
                if (method.ReturnType != typeof (void))
                {
                    resultType = typeof (ResponseMessage<>).MakeGenericType(method.ReturnType);
                    resultProp = resultType.GetProperty("R");
                }

                //var bchAgents = _service.GetBCHAgents(filter);
                //var result = JsonConvert.SerializeObject(bchAgents);
                consumerInfo.Method = m =>
                {
                    object[] parameters = null;
                    if (parameterInfos.Count != 0)
                    {
                        var messageWrapper = JsonConvert.DeserializeObject<RequestMessage>(m);
                        parameters = new object[parameterInfos.Count];
                        if (parameters.Length == 1)
                        {
                            var messgStr = messageWrapper.P.Values.FirstOrDefault();
                            if (!string.IsNullOrEmpty(messgStr))
                                parameters[0] = JsonConvert.DeserializeObject(messgStr,
                                    parameterInfos.Values.First().ParameterType);
                        }
                        else
                        {
                            foreach (var pair in messageWrapper.P.Where(pair => !string.IsNullOrEmpty(pair.Value)))
                            {
                                var info = parameterInfos[pair.Key];
                                parameters[info.Position] = JsonConvert.DeserializeObject(pair.Value, info.ParameterType);
                            }
                        }
                    }

                    var methodResult = method.Invoke(_service, parameters);
                    if (resultType == null)
                        return string.Empty;

                    var result = Activator.CreateInstance(resultType);
                    resultProp?.SetValue(result, methodResult, null);
                    return JsonConvert.SerializeObject(result);
                };
            }
            return consumerInfo.Method(message);
        }

        public void Dispose()
        {
            foreach (var value in _consumers.Values)
                value.Consumer.Received -= ConsumerOnReceived;
            _channel?.Dispose();
            _connection?.Dispose();
            (_service as IDisposable)?.Dispose();
        }

        private class ConsumerInfo
        {
            public EventingBasicConsumer Consumer { get; set; }
            public Func<string, string> Method { get; set; }
        }
    }
}