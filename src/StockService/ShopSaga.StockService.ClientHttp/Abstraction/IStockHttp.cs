using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ShopSaga.StockService.Shared;

namespace ShopSaga.StockService.ClientHttp.Abstraction
{
    public interface IStockHttp
    {
        // Metodi per gestire i product
        Task<bool> ConfirmAllStockReservationsForOrderAsync(int orderId, CancellationToken cancellationToken = default);
        Task<bool> CancelAllStockReservationsForOrderAsync(int orderId, CancellationToken cancellationToken = default);
        Task<IEnumerable<StockReservationDTO>> GetStockReservationsByOrderAsync(int orderId, CancellationToken cancellationToken = default);
        Task<bool> IsProductAvailableAsync(Guid productId, int quantity, CancellationToken cancellationToken = default);
        Task<ProductDTO> GetProductAsync(Guid productId, CancellationToken cancellationToken = default);
        
        // Metodi per gestire reservations individuali
        Task<StockReservationDTO> ReserveStockAsync(ReserveStockDTO reserveStockDto, CancellationToken cancellationToken = default);
        Task<IEnumerable<StockReservationDTO>> ReserveMultipleStockAsync(IEnumerable<ReserveStockDTO> reserveStockDtos, CancellationToken cancellationToken = default);
        Task<bool> CancelStockReservationAsync(Guid reservationId, CancellationToken cancellationToken = default);
    }
}
