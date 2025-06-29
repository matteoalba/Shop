using ShopSaga.OrderService.ClientHttp.Abstraction;
using ShopSaga.OrderService.Shared;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace ShopSaga.OrderService.ClientHttp
{
    public class OrderHttp : IOrderHttp
    {
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonOptions;

        public OrderHttp(HttpClient httpClient)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            };
        }

        public async Task<OrderDTO?> GetOrderAsync(int orderId, CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await _httpClient.GetAsync($"Order/GetOrder/{orderId}", cancellationToken);
                
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<OrderDTO>(_jsonOptions, cancellationToken);
                }
                
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return null;
                }
                
                throw new HttpRequestException($"Errore nella chiamata API: {response.StatusCode} - {response.ReasonPhrase}");
            }
            catch (Exception ex) when (!(ex is HttpRequestException))
            {
                throw new HttpRequestException($"Errore durante la richiesta dell'ordine {orderId}: {ex.Message}", ex);
            }
        }

        public async Task<IEnumerable<OrderDTO>> GetAllOrdersAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await _httpClient.GetAsync("Order/GetAll", cancellationToken);
                
                if (response.IsSuccessStatusCode)
                {
                    var orders = await response.Content.ReadFromJsonAsync<IEnumerable<OrderDTO>>(_jsonOptions, cancellationToken);
                    return orders ?? new List<OrderDTO>();
                }
                
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return new List<OrderDTO>();
                }
                
                throw new HttpRequestException($"Errore nella chiamata API: {response.StatusCode} - {response.ReasonPhrase}");
            }
            catch (Exception ex) when (!(ex is HttpRequestException))
            {
                throw new HttpRequestException($"Errore durante la richiesta di tutti gli ordini: {ex.Message}", ex);
            }
        }

        public async Task<IEnumerable<OrderDTO>> GetOrdersByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await _httpClient.GetAsync($"Order/GetOrderByCustomerId/{customerId}", cancellationToken);
                
                if (response.IsSuccessStatusCode)
                {
                    var orders = await response.Content.ReadFromJsonAsync<IEnumerable<OrderDTO>>(_jsonOptions, cancellationToken);
                    return orders ?? new List<OrderDTO>();
                }
                
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return new List<OrderDTO>();
                }
                
                throw new HttpRequestException($"Errore nella chiamata API: {response.StatusCode} - {response.ReasonPhrase}");
            }
            catch (Exception ex) when (!(ex is HttpRequestException))
            {
                throw new HttpRequestException($"Errore durante la richiesta degli ordini per il cliente {customerId}: {ex.Message}", ex);
            }
        }

        public async Task<OrderDTO?> CreateOrderAsync(CreateOrderDTO createOrderDto, CancellationToken cancellationToken = default)
        {
            try
            {
                if (createOrderDto == null)
                    throw new ArgumentNullException(nameof(createOrderDto));

                var response = await _httpClient.PostAsJsonAsync("Order/CreateOrder", createOrderDto, _jsonOptions, cancellationToken);
                
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<OrderDTO>(_jsonOptions, cancellationToken);
                }
                
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new HttpRequestException($"Errore nella creazione dell'ordine: {response.StatusCode} - {errorContent}");
            }
            catch (Exception ex) when (!(ex is HttpRequestException))
            {
                throw new HttpRequestException($"Errore durante la creazione dell'ordine: {ex.Message}", ex);
            }
        }

        public async Task<OrderDTO?> UpdateOrderAsync(int orderId, UpdateOrderDTO updateOrderDto, CancellationToken cancellationToken = default)
        {
            try
            {
                if (updateOrderDto == null)
                    throw new ArgumentNullException(nameof(updateOrderDto));

                var response = await _httpClient.PutAsJsonAsync($"Order/UpdateOrder/{orderId}", updateOrderDto, _jsonOptions, cancellationToken);
                
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<OrderDTO>(_jsonOptions, cancellationToken);
                }
                
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return null;
                }
                
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new HttpRequestException($"Errore nell'aggiornamento dell'ordine {orderId}: {response.StatusCode} - {errorContent}");
            }
            catch (Exception ex) when (!(ex is HttpRequestException))
            {
                throw new HttpRequestException($"Errore durante l'aggiornamento dell'ordine {orderId}: {ex.Message}", ex);
            }
        }

        public async Task<bool> DeleteOrderAsync(int orderId, CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"Order/DeleteOrder/{orderId}", cancellationToken);
                
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<bool>(_jsonOptions, cancellationToken);
                    return result;
                }
                
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return false;
                }
                
                throw new HttpRequestException($"Errore nella chiamata API: {response.StatusCode} - {response.ReasonPhrase}");
            }
            catch (Exception ex) when (!(ex is HttpRequestException))
            {
                throw new HttpRequestException($"Errore durante l'eliminazione dell'ordine {orderId}: {ex.Message}", ex);
            }
        }

        public async Task<bool> UpdateOrderStatusAsync(int orderId, string status, CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync($"Order/UpdateOrderStatus/{orderId}/status", new { Status = status }, _jsonOptions, cancellationToken);
                
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<bool>(_jsonOptions, cancellationToken);
                }
                
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return false;
                }
                
                throw new HttpRequestException($"Errore nella chiamata API: {response.StatusCode} - {response.ReasonPhrase}");
            }
            catch (Exception ex) when (!(ex is HttpRequestException))
            {
                throw new HttpRequestException($"Errore durante l'aggiornamento dello stato dell'ordine {orderId}: {ex.Message}", ex);
            }
        }
    }
}