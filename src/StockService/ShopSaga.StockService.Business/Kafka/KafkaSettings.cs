using System;

namespace ShopSaga.StockService.Business.Kafka
{
    public class KafkaSettings
    {
        public string BootstrapServers { get; set; }
        public string GroupId { get; set; }
        public string OrderCreatedTopic { get; set; }
        public int PollingIntervalSeconds { get; set; } = 10; // predef: 10 secondi
    }
}
