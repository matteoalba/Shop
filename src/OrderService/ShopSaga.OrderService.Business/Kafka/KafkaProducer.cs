using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Text.Json;
using System.Threading.Tasks;
using ShopSaga.OrderService.Business.Abstraction;

namespace ShopSaga.OrderService.Business.Kafka
{
    public class KafkaProducer : IKafkaProducer
    {
        private readonly ProducerConfig _config;
        private readonly ILogger<KafkaProducer> _logger;
        private readonly KafkaSettings _settings;

        public KafkaProducer(IOptions<KafkaSettings> settings, ILogger<KafkaProducer> logger)
        {
            _settings = settings.Value;
            _logger = logger;
            
            _config = new ProducerConfig
            {
                BootstrapServers = _settings.BootstrapServers
            };
        }

        public async Task<bool> ProduceAsync<T>(string topic, string key, T message)
        {
            try
            {
                using var producer = new ProducerBuilder<string, string>(_config).Build();
                
                var jsonMessage = JsonSerializer.Serialize(message);
                
                var result = await producer.ProduceAsync(topic, new Message<string, string> 
                { 
                    Key = key,
                    Value = jsonMessage
                });
                
                _logger.LogInformation("Messaggio pubblicato su {Topic}: {Key}, Status: {Status}", 
                    topic, key, result.Status);
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nella pubblicazione del messaggio su {Topic}: {Key}", topic, key);
                return false;
            }
        }
    }
}
