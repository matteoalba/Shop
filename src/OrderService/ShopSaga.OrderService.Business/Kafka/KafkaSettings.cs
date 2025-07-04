using System;

namespace ShopSaga.OrderService.Business.Kafka
{
    public class KafkaSettings
    {
        public string BootstrapServers { get; set; }
        public string GroupId { get; set; }
        public string OrderCreatedTopic { get; set; }
        public string OrderCancelledTopic { get; set; }
    }
}
