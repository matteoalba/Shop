using ShopSaga.StockService.Repository.Model;
using ShopSaga.StockService.Shared;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ShopSaga.OrderService.Shared.Events;

namespace ShopSaga.StockService.Business.Abstraction
{
    public interface IStockBusiness
    {
        // Gestione Prodotti
        Task<ProductDTO> GetProductAsync(Guid id, CancellationToken cancellationToken = default);
        Task<IEnumerable<ProductDTO>> GetAllProductsAsync(CancellationToken cancellationToken = default);
        Task<ProductDTO> CreateProductAsync(CreateProductDTO createProductDto, CancellationToken cancellationToken = default);
        Task<ProductDTO> UpdateProductAsync(Guid id, UpdateProductDTO updateProductDto, CancellationToken cancellationToken = default);
        Task<bool> DeleteProductAsync(Guid id, CancellationToken cancellationToken = default);
        Task<bool> IsProductAvailableAsync(Guid productId, int quantity, CancellationToken cancellationToken = default);

        // Gestione Stock Reservations (per SAGA)
        Task<StockReservationDTO> ReserveStockAsync(ReserveStockDTO reserveStockDto, CancellationToken cancellationToken = default);
        Task<bool> ConfirmAllStockReservationsForOrderAsync(int orderId, CancellationToken cancellationToken = default);
        Task<bool> CancelStockReservationAsync(Guid reservationId, CancellationToken cancellationToken = default);
        Task<StockReservationDTO> GetStockReservationAsync(Guid reservationId, CancellationToken cancellationToken = default);
        Task<IEnumerable<StockReservationDTO>> GetStockReservationsByOrderAsync(int orderId, CancellationToken cancellationToken = default);

        // Operazioni batch per ordini complessi
        Task<IEnumerable<StockReservationDTO>> ReserveMultipleStockAsync(IEnumerable<ReserveStockDTO> reserveStockDtos, CancellationToken cancellationToken = default);
        Task<bool> CancelAllStockReservationsForOrderAsync(int orderId, CancellationToken cancellationToken = default);

        // Query utili
        Task<int> GetAvailableStockAsync(Guid productId, CancellationToken cancellationToken = default);
        
        // Kafka
        Task ProcessOrderCreatedEventAsync(OrderCreatedEvent orderCreatedEvent, CancellationToken cancellationToken = default);
    }
}