using Microsoft.Extensions.Logging;
using ShopSaga.PaymentService.ClientHttp.Abstraction;
using ShopSaga.PaymentService.Shared;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace ShopSaga.PaymentService.ClientHttp
{
    public class PaymentHttp : IPaymentHttp
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<PaymentHttp> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        public PaymentHttp(HttpClient httpClient, ILogger<PaymentHttp> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            };
        }

        public async Task<PaymentDTO> GetPaymentByOrderIdAsync(int orderId, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Chiamata HTTP a PaymentService per ottenere pagamento dell'ordine {OrderId}", orderId);
                
                var response = await _httpClient.GetAsync($"Payment/GetPaymentByOrderId/order/{orderId}", cancellationToken);
                
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<PaymentDTO>(_jsonOptions, cancellationToken);
                    _logger.LogInformation("Pagamento ordine {OrderId} recuperato: Status={Status}, Amount={Amount}", 
                        orderId, result?.Status ?? "Unknown", result?.Amount ?? 0);
                    return result;
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _logger.LogInformation("Nessun pagamento trovato per ordine {OrderId}", orderId);
                    return null;
                }
                else
                {
                    _logger.LogWarning("Errore HTTP durante recupero pagamento ordine {OrderId}: {StatusCode}", orderId, response.StatusCode);
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante la chiamata HTTP per recuperare pagamento ordine {OrderId}", orderId);
                return null;
            }
        }

        public async Task<bool> IsPaymentProcessedAsync(int orderId, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Chiamata HTTP a PaymentService per verificare se ordine {OrderId} è stato processato", orderId);
                
                var response = await _httpClient.GetAsync($"Payment/IsPaymentProcessed/order/{orderId}/processed", cancellationToken);
                
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<bool>(_jsonOptions, cancellationToken);
                    _logger.LogInformation("Ordine {OrderId} processato: {IsProcessed}", orderId, result);
                    return result;
                }
                else
                {
                    _logger.LogWarning("Errore HTTP durante verifica processamento ordine {OrderId}: {StatusCode}", orderId, response.StatusCode);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante la chiamata HTTP per verificare processamento ordine {OrderId}", orderId);
                return false;
            }
        }

        public async Task<PaymentDTO> GetPaymentAsync(int paymentId, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Chiamata HTTP a PaymentService per ottenere pagamento {PaymentId}", paymentId);
                
                var response = await _httpClient.GetAsync($"Payment/GetPayment/{paymentId}", cancellationToken);
                
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<PaymentDTO>(_jsonOptions, cancellationToken);
                    _logger.LogInformation("Pagamento {PaymentId} recuperato: Status={Status}", paymentId, result?.Status ?? "Unknown");
                    return result;
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _logger.LogInformation("Pagamento {PaymentId} non trovato", paymentId);
                    return null;
                }
                else
                {
                    _logger.LogWarning("Errore HTTP durante recupero pagamento {PaymentId}: {StatusCode}", paymentId, response.StatusCode);
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante la chiamata HTTP per recuperare pagamento {PaymentId}", paymentId);
                return null;
            }
        }

        public async Task<IEnumerable<PaymentDTO>> GetPaymentsByStatusAsync(string status, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Chiamata HTTP a PaymentService per ottenere pagamenti con status {Status}", status);
                
                var response = await _httpClient.GetAsync($"Payment/GetPaymentsByStatus/status/{status}", cancellationToken);
                
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<IEnumerable<PaymentDTO>>(_jsonOptions, cancellationToken);
                    _logger.LogInformation("Recuperati {Count} pagamenti con status {Status}", result?.Count() ?? 0, status);
                    return result ?? new List<PaymentDTO>();
                }
                else
                {
                    _logger.LogWarning("Errore HTTP durante recupero pagamenti per status {Status}: {StatusCode}", status, response.StatusCode);
                    return new List<PaymentDTO>();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante la chiamata HTTP per recuperare pagamenti con status {Status}", status);
                return new List<PaymentDTO>();
            }
        }

        public async Task<bool> HasPendingPaymentAsync(int orderId, CancellationToken cancellationToken = default)
        {
            try
            {
                var payment = await GetPaymentByOrderIdAsync(orderId, cancellationToken);
                var hasPending = payment != null && payment.Status == "Pending";
                
                _logger.LogInformation("Ordine {OrderId} ha pagamento pending: {HasPending}", orderId, hasPending);
                return hasPending;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante verifica pagamento pending per ordine {OrderId}", orderId);
                return false;
            }
        }

        public async Task<bool> IsOrderFullyPaidAsync(int orderId, CancellationToken cancellationToken = default)
        {
            try
            {
                var payment = await GetPaymentByOrderIdAsync(orderId, cancellationToken);
                var isFullyPaid = payment != null && payment.Status == "Completed";
                
                _logger.LogInformation("Ordine {OrderId} completamente pagato: {IsFullyPaid}", orderId, isFullyPaid);
                return isFullyPaid;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante verifica pagamento completo per ordine {OrderId}", orderId);
                return false;
            }
        }
    }
}
