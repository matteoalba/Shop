using Microsoft.Extensions.Logging;
using ShopSaga.StockService.ClientHttp.Abstraction;
using ShopSaga.StockService.Shared;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace ShopSaga.StockService.ClientHttp
{
    /// <summary>
    /// Client HTTP per comunicare con il Stock Service
    /// </summary>
    public class StockHttp : IStockHttp
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<StockHttp> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        public StockHttp(HttpClient httpClient, ILogger<StockHttp> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            };
        }

        /// <summary>
        /// Conferma definitivamente tutte le prenotazioni di stock per un ordine
        /// Utilizzato quando il pagamento è andato a buon fine
        /// </summary>
        public async Task<bool> ConfirmAllStockReservationsForOrderAsync(int orderId, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Chiamata HTTP a StockService per confermare tutte le prenotazioni dell'ordine {OrderId}", orderId);
                
                var response = await _httpClient.PutAsync($"Stock/ConfirmAllStockReservationsForOrder/order/{orderId}/reservations/confirm", null, cancellationToken);
                
                if (response.IsSuccessStatusCode)
                {
                    // Leggi il contenuto come stringa per gestire risposte non-JSON
                    var content = await response.Content.ReadAsStringAsync(cancellationToken);
                    bool result;
                    
                    // Prova prima a deserializzare come JSON, poi fallback su stringa
                    if (content.Trim().StartsWith("{") || content.Trim().Equals("true", StringComparison.OrdinalIgnoreCase) || content.Trim().Equals("false", StringComparison.OrdinalIgnoreCase))
                    {
                        if (bool.TryParse(content.Trim(), out result))
                        {
                            // Parsing come boolean riuscito
                        }
                        else
                        {
                            // Prova parsing JSON
                            try
                            {
                                result = JsonSerializer.Deserialize<bool>(content, _jsonOptions);
                            }
                            catch
                            {
                                // Se tutto fallisce, considera successo se status è 200
                                result = true;
                            }
                        }
                    }
                    else
                    {
                        // Fallback: considera successo se status è 200
                        result = true;
                    }
                    
                    _logger.LogInformation("Conferma prenotazioni stock ordine {OrderId}: {Result}", orderId, result ? "Success" : "Failed");
                    return result;
                }
                else
                {
                    _logger.LogWarning("Errore HTTP durante conferma prenotazioni stock ordine {OrderId}: {StatusCode}", orderId, response.StatusCode);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante la chiamata HTTP per confermare prenotazioni stock ordine {OrderId}", orderId);
                return false;
            }
        }

        /// <summary>
        /// Cancella tutte le prenotazioni di stock per un ordine e ripristina la disponibilità
        /// Utilizzato in caso di fallimento del pagamento o cancellazione ordine
        /// </summary>
        public async Task<bool> CancelAllStockReservationsForOrderAsync(int orderId, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Chiamata HTTP a StockService per cancellare tutte le prenotazioni dell'ordine {OrderId}", orderId);
                
                var response = await _httpClient.PutAsync($"Stock/CancelAllStockReservationsForOrder/reservations/order/{orderId}/cancel-all", null, cancellationToken);
                
                if (response.IsSuccessStatusCode)
                {
                    // Leggi il contenuto come stringa per gestire risposte non-JSON
                    var content = await response.Content.ReadAsStringAsync(cancellationToken);
                    bool result;
                    
                    // Prova prima a deserializzare come JSON, poi fallback su stringa
                    if (content.Trim().StartsWith("{") || content.Trim().Equals("true", StringComparison.OrdinalIgnoreCase) || content.Trim().Equals("false", StringComparison.OrdinalIgnoreCase))
                    {
                        if (bool.TryParse(content.Trim(), out result))
                        {
                            // Parsing come boolean riuscito
                        }
                        else
                        {
                            // Prova parsing JSON
                            try
                            {
                                result = JsonSerializer.Deserialize<bool>(content, _jsonOptions);
                            }
                            catch
                            {
                                // Se tutto fallisce, considera successo se status è 200
                                result = true;
                            }
                        }
                    }
                    else
                    {
                        // Fallback: considera successo se status è 200
                        result = true;
                    }
                    
                    _logger.LogInformation("Cancellazione prenotazioni stock ordine {OrderId}: {Result}", orderId, result ? "Success" : "Failed");
                    return result;
                }
                else
                {
                    _logger.LogWarning("Errore HTTP durante cancellazione prenotazioni stock ordine {OrderId}: {StatusCode}", orderId, response.StatusCode);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante la chiamata HTTP per cancellare prenotazioni stock ordine {OrderId}", orderId);
                return false;
            }
        }

        public async Task<IEnumerable<StockReservationDTO>> GetStockReservationsByOrderAsync(int orderId, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Chiamata HTTP a StockService per ottenere prenotazioni dell'ordine {OrderId}", orderId);
                
                var response = await _httpClient.GetAsync($"Stock/GetStockReservationsByOrder/reservations/order/{orderId}", cancellationToken);
                
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<IEnumerable<StockReservationDTO>>(_jsonOptions, cancellationToken);
                    _logger.LogInformation("Ottenute {Count} prenotazioni stock per ordine {OrderId}", result?.Count() ?? 0, orderId);
                    return result ?? new List<StockReservationDTO>();
                }
                else
                {
                    // In caso di errore HTTP restituiamo una lista vuota per evitare null reference
                    _logger.LogWarning("Errore HTTP durante recupero prenotazioni stock ordine {OrderId}: {StatusCode}", orderId, response.StatusCode);
                    return new List<StockReservationDTO>();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante la chiamata HTTP per recuperare prenotazioni stock ordine {OrderId}", orderId);
                return new List<StockReservationDTO>();
            }
        }

        /// <summary>
        /// Verifica se un prodotto ha abbastanza stock disponibile per la quantità richiesta
        /// Non effettua prenotazioni, solo controllo di disponibilità
        /// </summary>
        public async Task<bool> IsProductAvailableAsync(Guid productId, int quantity, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Chiamata HTTP a StockService per verificare disponibilità prodotto {ProductId}, quantità {Quantity}", productId, quantity);
                
                var response = await _httpClient.GetAsync($"Stock/CheckProductAvailability/products/{productId}/availability?quantity={quantity}", cancellationToken);
                
                if (response.IsSuccessStatusCode)
                {
                    // Leggi il contenuto come stringa per gestire risposte non-JSON
                    var content = await response.Content.ReadAsStringAsync(cancellationToken);
                    bool result;
                    
                    // Prova prima a deserializzare come JSON, poi fallback su stringa
                    if (content.Trim().StartsWith("{") || content.Trim().Equals("true", StringComparison.OrdinalIgnoreCase) || content.Trim().Equals("false", StringComparison.OrdinalIgnoreCase))
                    {
                        if (bool.TryParse(content.Trim(), out result))
                        {
                            // Parsing come boolean riuscito
                        }
                        else
                        {
                            // Prova parsing JSON
                            try
                            {
                                result = JsonSerializer.Deserialize<bool>(content, _jsonOptions);
                            }
                            catch
                            {
                                // Se tutto fallisce, considera non disponibile per sicurezza
                                result = false;
                            }
                        }
                    }
                    else
                    {
                        // Fallback: considera non disponibile per sicurezza
                        result = false;
                    }
                    
                    _logger.LogInformation("Disponibilità prodotto {ProductId} (qty {Quantity}): {Available}", productId, quantity, result);
                    return result;
                }
                else
                {
                    _logger.LogWarning("Errore HTTP durante verifica disponibilità prodotto {ProductId}: {StatusCode}", productId, response.StatusCode);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante la chiamata HTTP per verificare disponibilità prodotto {ProductId}", productId);
                return false;
            }
        }

        public async Task<ProductDTO> GetProductAsync(Guid productId, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Chiamata HTTP a StockService per ottenere prodotto {ProductId}", productId);
                
                var response = await _httpClient.GetAsync($"Stock/GetProduct/products/{productId}", cancellationToken);
                
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<ProductDTO>(_jsonOptions, cancellationToken);
                    _logger.LogInformation("Prodotto {ProductId} recuperato: {ProductName}", productId, result?.Name ?? "Unknown");
                    return result;
                }
                else
                {
                    // Prodotto non trovato o errore nel servizio
                    _logger.LogWarning("Errore HTTP durante recupero prodotto {ProductId}: {StatusCode}", productId, response.StatusCode);
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante la chiamata HTTP per recuperare prodotto {ProductId}", productId);
                return null;
            }
        }

        /// <summary>
        /// Prenota stock per un singolo prodotto, decrementando la disponibilità
        /// La prenotazione rimane attiva fino a conferma o cancellazione
        /// </summary>
        public async Task<StockReservationDTO> ReserveStockAsync(ReserveStockDTO reserveStockDto, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Chiamata HTTP a StockService per prenotare stock: Prodotto {ProductId}, Ordine {OrderId}, Quantità {Quantity}", 
                    reserveStockDto.ProductId, reserveStockDto.OrderId, reserveStockDto.Quantity);
                
                var response = await _httpClient.PostAsJsonAsync("Stock/ReserveStock/reservations", reserveStockDto, _jsonOptions, cancellationToken);
                
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<StockReservationDTO>(_jsonOptions, cancellationToken);
                    _logger.LogInformation("Prenotazione stock creata: ID {ReservationId} per Prodotto {ProductId}", 
                        result?.Id, reserveStockDto.ProductId);
                    return result;
                }
                else
                {
                    // Prenotazione fallita - potrebbe essere stock insufficiente
                    _logger.LogWarning("Errore HTTP durante prenotazione stock: {StatusCode}", response.StatusCode);
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante la chiamata HTTP per prenotare stock Prodotto {ProductId}", reserveStockDto.ProductId);
                return null;
            }
        }

        /// <summary>
        /// Prenota stock per più prodotti in una singola operazione atomica
        /// Utile per ordini con più articoli - tutto riesce o tutto fallisce
        /// </summary>
        public async Task<IEnumerable<StockReservationDTO>> ReserveMultipleStockAsync(IEnumerable<ReserveStockDTO> reserveStockDtos, CancellationToken cancellationToken = default)
        {
            try
            {
                var dtosList = reserveStockDtos.ToList();
                _logger.LogInformation("Chiamata HTTP a StockService per prenotazioni multiple: {Count} items", dtosList.Count);
                
                var response = await _httpClient.PostAsJsonAsync("Stock/ReserveMultipleStock/reservations/batch", dtosList, _jsonOptions, cancellationToken);
                
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<IEnumerable<StockReservationDTO>>(_jsonOptions, cancellationToken);
                    _logger.LogInformation("Prenotazioni multiple create: {Count} prenotazioni", result?.Count() ?? 0);
                    return result ?? new List<StockReservationDTO>();
                }
                else
                {
                    // Operazione batch fallita - nessuna prenotazione creata
                    _logger.LogWarning("Errore HTTP durante prenotazioni multiple: {StatusCode}", response.StatusCode);
                    return new List<StockReservationDTO>();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante la chiamata HTTP per prenotazioni multiple");
                return new List<StockReservationDTO>();
            }
        }

        /// <summary>
        /// Cancella una prenotazione di stock specifica e ripristina la disponibilità
        /// </summary>
        public async Task<bool> CancelStockReservationAsync(Guid reservationId, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Chiamata HTTP a StockService per cancellare prenotazione {ReservationId}", reservationId);
                
                var response = await _httpClient.PutAsync($"Stock/CancelStockReservation/reservations/{reservationId}/cancel", null, cancellationToken);
                
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Prenotazione {ReservationId} cancellata con successo", reservationId);
                    return true;
                }
                else
                {
                    _logger.LogWarning("Errore HTTP durante cancellazione prenotazione {ReservationId}: {StatusCode}", reservationId, response.StatusCode);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante la chiamata HTTP per cancellare prenotazione {ReservationId}", reservationId);
                return false;
            }
        }
    }
}
