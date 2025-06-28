using ShopSaga.StockService.Repository.Model;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ShopSaga.StockService.Repository.Abstraction
{
    public interface IStockRepository
    {
        // Metodi per Product
        Task<Product?> GetProductByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<IEnumerable<Product>> GetAllProductsAsync(CancellationToken cancellationToken = default);
        Task<Product> CreateProductAsync(Product product, CancellationToken cancellationToken = default);
        Task<Product?> UpdateProductAsync(Product product, CancellationToken cancellationToken = default);
        Task<bool> DeleteProductAsync(Guid id, CancellationToken cancellationToken = default);

        // Metodi per StockReservation
        Task<StockReservation?> GetStockReservationByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<IEnumerable<StockReservation>> GetStockReservationsByOrderIdAsync(int orderId, CancellationToken cancellationToken = default);
        Task<StockReservation?> GetStockReservationByOrderAndProductAsync(int orderId, Guid productId, CancellationToken cancellationToken = default);
        Task<StockReservation> CreateStockReservationAsync(int orderId, Guid productId, int quantity, CancellationToken cancellationToken = default);
        Task<bool> ConfirmStockReservationAsync(Guid reservationId, CancellationToken cancellationToken = default);
        Task<bool> CancelStockReservationAsync(Guid reservationId, CancellationToken cancellationToken = default);

        // Salvataggio
        Task<int> SaveChanges(CancellationToken cancellationToken = default);
    }
}