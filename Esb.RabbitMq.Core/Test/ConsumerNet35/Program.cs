using System;
using System.Collections.Generic;
using System.Text;
using ConsumerNet35.Contracts;
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
			Console.WriteLine($"exchange: {ea.Exchange}; rout: {ea.RoutingKey}; correlationId: {props.CorrelationId}; replyTo: {props.ReplyTo}");
			var message = Encoding.UTF8.GetString(ea.Body);
			Console.WriteLine("message: " + message);
			_channel.BasicAck(ea.DeliveryTag, false);
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
			throw new System.NotImplementedException();
		}

		public PartnerData GetAgentDetailsByCode(string code)
		{
			throw new System.NotImplementedException();
		}
	}
}
