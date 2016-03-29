using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Esb.RabbitMq.Core.Contracts;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace ConsumerNet35
{
    class Program
    {
        static void Main(string[] args)
        {
            using (new QueueHost<ICMSService>(new ExperimentalImplementation()))
            {
                Console.WriteLine("start listening");
                Console.ReadKey();
            }

            Console.WriteLine("work done...");
        }
    }

    public class QueueHost<T> : IDisposable where T : class
    {
        private readonly IConnection _connection;
        private readonly IModel _channel;
        private readonly Dictionary<string, EventingBasicConsumer> _consumers = new Dictionary<string, EventingBasicConsumer>();
        private readonly T _service;

        public QueueHost(T service)
        {
            _service = service;
            var factory = new ConnectionFactory { HostName = "localhost", UserName = "guest", Password = "guest" };
            var type = typeof(T);

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();
            _channel.BasicQos(0, 1, false);
            _channel.ExchangeDeclare(type.Name, "direct");

            var methods = type.GetMethods();
            foreach (var methodInfo in methods)
            {
                var queue = _channel.QueueDeclare(methodInfo.Name, false, false, false, null);
                if (queue != null)
                {
                    _channel.QueueBind(queue.QueueName, type.Name, methodInfo.Name);
                    var consumer = new EventingBasicConsumer(_channel);
                    consumer.Received += ConsumerOnReceived;
                    _consumers.Add(queue.QueueName, consumer);
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

                if (ea.RoutingKey == "GetBCHAgents")
                {
                    var filter = JsonConvert.DeserializeObject<AgencyFilter>(message);
                    var bchAgents = service.GetBCHAgents(filter);
                    response = JsonConvert.SerializeObject(bchAgents);
                }
                else if (ea.RoutingKey == "GetAgentDetailsByCode")
                {
                    var code = JsonConvert.DeserializeObject<string>(message);
                    var details = service.GetAgentDetailsByCode(code);
                    response = JsonConvert.SerializeObject(details);
                }

            }
            catch (Exception ex)
            {
                response = "";
                Console.WriteLine(ex.Message);
            }
            finally
            {
                _channel.BasicPublish("", props.ReplyTo, replyProps, Encoding.UTF8.GetBytes(response));
                _channel.BasicAck(ea.DeliveryTag, false);
            }
        }

        public void Dispose()
        {
            foreach (var value in _consumers.Values)
                value.Received -= ConsumerOnReceived;
            _channel?.Dispose();
            _connection?.Dispose();
            (_service as IDisposable)?.Dispose();
        }
    }


    public class ExperimentalImplementation : ICMSService
    {
        //public Func<AgencyFilter, BlockAgentInfo[]> GetBCHAgents { get; set; }
        public BlockAgentInfo[] GetBCHAgents(AgencyFilter filter)
        {
            Console.WriteLine($"codes: [{string.Join(",", filter.Codes)}], types: [{string.Join(",", filter.ContractTypes.Select(t => t.ToString()).ToArray())}], isActiveOnly: {filter.IsActiveOnly}");
            return new[]
            {
                new BlockAgentInfo
                {
                    AgentCode = filter.Codes.FirstOrDefault() ?? "555",
                    Agreements = new[] {"asdf"}
                },
                new BlockAgentInfo
                {
                    AgentCode = filter.Codes.LastOrDefault() ?? "555",
                    Agreements = new[] {"qwer"}
                }
            };
        }

        public PartnerData GetAgentDetailsByCode(string code)
        {
            Console.WriteLine("code: " + code);
            return new PartnerData
            {
                Code = code,
                Id = 999,
                PartnerGuid = Guid.NewGuid(),
                BankDetailsCommon = new PartnerBankDetailsCommon
                {
                    Inn = "11112222333344445555",
                    Vat = "0534654"
                },
                AgreementList = new List<PartnerDocument>
                {
                    new PartnerDocument
                    {
                        StatusReason = ContractStatusReason.Canceled,
                        DocumentStartDate = null,
                        BankDetails = new PartnerBankDetails
                        {
                            BankName = "Alfa-bank",
                            Bic = "9876543210"
                        }
                    },
                    new PartnerDocument
                    {
                        StatusReason = ContractStatusReason.Paused,
                        DocumentStartDate = DateTime.Now,
                        BankDetails = new PartnerBankDetails
                        {
                            BankName = "ООО \"Рога и копыта\"",
                            Bic = "0123456789"
                        }
                    }
                }
            };
        }
    }
}
