using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ShopSaga.OrderService.Shared.Events;
using ShopSaga.StockService.Business.Abstraction;

namespace ShopSaga.StockService.Business.Kafka
{
    public class KafkaConsumerService : BackgroundService
    {
        private readonly IConsumer<string, string> _consumer;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<KafkaConsumerService> _logger;
        private readonly KafkaSettings _settings;
        private readonly TimeSpan _pollingInterval;

        public KafkaConsumerService(
            IOptions<KafkaSettings> settings,
            IServiceProvider serviceProvider,
            ILogger<KafkaConsumerService> logger)
        {
            _settings = settings.Value;
            _serviceProvider = serviceProvider;
            _logger = logger;
            _pollingInterval = TimeSpan.FromSeconds(_settings.PollingIntervalSeconds);

            if (string.IsNullOrEmpty(_settings.BootstrapServers))
            {
                _logger.LogWarning("BootstrapServers non configurato. Utilizzo del valore predefinito localhost:9092");
                _settings.BootstrapServers = "localhost:9092";
            }
            if (string.IsNullOrEmpty(_settings.GroupId))
            {
                _logger.LogWarning("GroupId non configurato. Utilizzo del valore predefinito stock-service-group");
                _settings.GroupId = "stock-service-group";
            }
            if (string.IsNullOrEmpty(_settings.OrderCreatedTopic))
            {
                _logger.LogWarning("OrderCreatedTopic non configurato. Utilizzo del valore predefinito order-created");
                _settings.OrderCreatedTopic = "order-created";
            }
            _logger.LogInformation("Configurazione Kafka: BootstrapServers={BootstrapServers}, GroupId={GroupId}, Topic={Topic}",
                _settings.BootstrapServers, _settings.GroupId, _settings.OrderCreatedTopic);

            var config = new ConsumerConfig
            {
                BootstrapServers = _settings.BootstrapServers,
                GroupId = _settings.GroupId,
                AutoOffsetReset = AutoOffsetReset.Latest,
                EnableAutoCommit = false,
                SocketConnectionSetupTimeoutMs = 10000,
                SocketTimeoutMs = 30000,
                ConnectionsMaxIdleMs = 60000,
                SessionTimeoutMs = 30000,
                MaxPollIntervalMs = 300000,
            };

            try
            {
                _consumer = new ConsumerBuilder<string, string>(config).Build();
                _logger.LogInformation("Consumer Kafka creato con successo");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante la creazione del consumer Kafka");
                throw;
            }
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Avvio il consumer in un task separato per non bloccare l'applicazione
            Task.Run(() => StartKafkaConsumerAsync(stoppingToken), stoppingToken);
            return Task.CompletedTask;
        }

        private async Task StartKafkaConsumerAsync(CancellationToken stoppingToken)
        {
            await Task.Delay(5000, stoppingToken);
            _logger.LogInformation("Tentativo di connessione a Kafka con BootstrapServers: {BootstrapServers}, GroupId: {GroupId}, Topic: {Topic}",
                _settings.BootstrapServers, _settings.GroupId, _settings.OrderCreatedTopic);

            bool isKafkaConnected = false;
            int retryCount = 0;

            while (!isKafkaConnected && !stoppingToken.IsCancellationRequested && retryCount < 5)
            {
                try
                {
                    _consumer.Subscribe(_settings.OrderCreatedTopic);
                    isKafkaConnected = true;
                    _logger.LogInformation("Kafka consumer avviato. In ascolto sul topic: {Topic}", _settings.OrderCreatedTopic);
                }
                catch (Exception ex)
                {
                    retryCount++;
                    _logger.LogWarning(ex, "Tentativo {RetryCount}/5 fallito di connessione a Kafka: {Error}", retryCount, ex.Message);
                    await Task.Delay(2000, stoppingToken);
                }
            }

            if (!isKafkaConnected && !stoppingToken.IsCancellationRequested)
            {
                _logger.LogError("Impossibile connettersi a Kafka dopo 5 tentativi. Il servizio continuerà senza consumer Kafka.");
                return;
            }

            try
            {
                _logger.LogInformation("Avvio del polling periodico con intervallo di {PollingInterval} secondi", _pollingInterval.TotalSeconds);
                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        // Ogni ciclo controlla se ci sono nuovi messaggi da processare
                        int processedCount = await ConsumeAvailableMessagesAsync(stoppingToken);
                        if (processedCount > 0)
                        {
                            _logger.LogInformation("Elaborati {Count} messaggi durante questo ciclo di polling", processedCount);
                        }
                        await Task.Delay(_pollingInterval, stoppingToken);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Errore durante il ciclo di polling Kafka");
                        await Task.Delay(5000, stoppingToken);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore critico durante l'inizializzazione del consumer Kafka");
            }
            finally
            {
                try
                {
                    _consumer.Close();
                    _logger.LogInformation("Kafka consumer chiuso correttamente");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Errore durante la chiusura del consumer Kafka");
                }
            }
        }

        private async Task<int> ConsumeAvailableMessagesAsync(CancellationToken cancellationToken)
        {
            int messagesProcessed = 0;
            bool hasMoreMessages = true;
            int maxBatchSize = 100;
            try
            {
                while (hasMoreMessages && messagesProcessed < maxBatchSize && !cancellationToken.IsCancellationRequested)
                {
                    // Consuma tutti i messaggi disponibili in questo ciclo
                    var consumeResult = _consumer.Consume(TimeSpan.FromMilliseconds(100));
                    if (consumeResult != null)
                    {
                        _logger.LogInformation("Messaggio ricevuto da {Topic}: {Key}",
                            consumeResult.Topic, consumeResult.Message.Key);
                        _logger.LogInformation("Elaborazione messaggio con chiave {Key}, offset {Offset} da {Topic}...",
                            consumeResult.Message.Key, consumeResult.Offset, consumeResult.Topic);
                        await ProcessMessageAsync(consumeResult.Message.Value, cancellationToken);
                        _consumer.Commit(consumeResult);
                        _logger.LogInformation("Offset committato per {Topic}, Partition: {Partition}, Offset: {Offset}",
                            consumeResult.Topic, consumeResult.Partition, consumeResult.Offset);
                        messagesProcessed++;
                    }
                    else
                    {
                        hasMoreMessages = false;
                    }
                }
                return messagesProcessed;
            }
            catch (ConsumeException ex)
            {
                _logger.LogError(ex, "Errore di consumo Kafka durante il polling: {ErrorReason}", ex.Error.Reason);
                return messagesProcessed;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante il polling dei messaggi Kafka");
                return messagesProcessed;
            }
        }

        private async Task ProcessMessageAsync(string message, CancellationToken cancellationToken)
        {
            try
            {
                // Qui deserializziamo l'evento e invochiamo la business logic
                var orderCreatedEvent = JsonSerializer.Deserialize<OrderCreatedEvent>(message);
                using var scope = _serviceProvider.CreateScope();
                var stockBusiness = scope.ServiceProvider.GetRequiredService<IStockBusiness>();
                await stockBusiness.ProcessOrderCreatedEventAsync(orderCreatedEvent, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante l'elaborazione del messaggio: {Message}", message);
            }
        }
    }
}
