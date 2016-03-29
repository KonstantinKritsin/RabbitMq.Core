using System;
using Esb.RabbitMq.Core.Contracts;

namespace SubscriberNet35
{
    public interface IQueueServiceFactory<out T> : IDisposable where T : class
    {
        T CreateService();
    }
}