using ShopSaga.OrderService.Repository.Model;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ShopSaga.OrderService.Shared;

namespace ShopSaga.OrderService.Business.Abstraction
{
    public interface IKafkaProducer
    {
        Task<bool> ProduceAsync<T>(string topic, string key, T message);
    }
}

