using Microsoft.EntityFrameworkCore;
using ShopSaga.StockService.Repository.Abstraction;
using ShopSaga.StockService.Repository.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ShopSaga.StockService.Repository
{
    public class StockRepository : IStockRepository
    {
        private readonly StockDbContext _context;

        public StockRepository(StockDbContext context)
        {
            _context = context;
        }

        public async Task<int> SaveChanges(CancellationToken cancellationToken = default)
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }

        // Metodi per Product
        public async Task<Product?> GetProductByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.Products.FindAsync(new object[] { id }, cancellationToken);
        }

        public async Task<IEnumerable<Product>> GetAllProductsAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Products.ToListAsync(cancellationToken);
        }

        public async Task<Product> CreateProductAsync(Product product, CancellationToken cancellationToken = default)
        {
            product.Id = Guid.NewGuid();
            product.CreatedAt = DateTime.UtcNow;
            product.UpdatedAt = DateTime.UtcNow;

            _context.Products.Add(product);
            return product;
        }

        public async Task<Product?> UpdateProductAsync(Product product, CancellationToken cancellationToken = default)
        {
            var existingProduct = await GetProductByIdAsync(product.Id, cancellationToken);
            if (existingProduct == null)
                return null;

            existingProduct.Name = product.Name;
            existingProduct.Description = product.Description;
            existingProduct.Price = product.Price;
            existingProduct.QuantityInStock = product.QuantityInStock;
            existingProduct.UpdatedAt = DateTime.UtcNow;

            return existingProduct;
        }

        public async Task<bool> DeleteProductAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var product = await GetProductByIdAsync(id, cancellationToken);
            if (product == null)
                return false;

            _context.Products.Remove(product);
            return true;
        }

        // Metodi per StockReservation
        public async Task<StockReservation?> GetStockReservationByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.StockReservations
                .Include(sr => sr.Product)
                .FirstOrDefaultAsync(sr => sr.Id == id, cancellationToken);
        }

        public async Task<IEnumerable<StockReservation>> GetStockReservationsByOrderIdAsync(int orderId, CancellationToken cancellationToken = default)
        {
            return await _context.StockReservations
                .Include(sr => sr.Product)
                .Where(sr => sr.OrderId == orderId)
                .ToListAsync(cancellationToken);
        }

        public async Task<StockReservation?> GetStockReservationByOrderAndProductAsync(int orderId, Guid productId, CancellationToken cancellationToken = default)
        {
            return await _context.StockReservations
                .Include(sr => sr.Product)
                .FirstOrDefaultAsync(sr => sr.OrderId == orderId && sr.ProductId == productId && sr.Status == "Reserved", cancellationToken);
        }

        public async Task<StockReservation> CreateStockReservationAsync(int orderId, Guid productId, int quantity, CancellationToken cancellationToken = default)
        {
            var product = await GetProductByIdAsync(productId, cancellationToken);
            if (product == null)
                throw new ArgumentException($"Prodotto con ID {productId} non trovato");

            // Verifica se esiste già una prenotazione per questo ordine e prodotto
            var existingReservation = await GetStockReservationByOrderAndProductAsync(orderId, productId, cancellationToken);
            
            if (existingReservation != null)
            {
                // Verifica se c'è abbastanza stock per la quantità aggiuntiva
                if (product.QuantityInStock < quantity)
                    throw new InvalidOperationException($"Stock insufficiente per il prodotto {product.Name}. Disponibile: {product.QuantityInStock}, Richiesto: {quantity}");

                // Aggiorna la quantità della prenotazione esistente
                existingReservation.Quantity += quantity;
                existingReservation.UpdatedAt = DateTime.UtcNow;

                // Riduci lo stock del prodotto
                product.QuantityInStock -= quantity;
                product.UpdatedAt = DateTime.UtcNow;
                return existingReservation;
            }
            else
            {
                // Verifica se c'è abbastanza stock per la nuova prenotazione
                if (product.QuantityInStock < quantity)
                    throw new InvalidOperationException($"Stock insufficiente per il prodotto {product.Name}. Disponibile: {product.QuantityInStock}, Richiesto: {quantity}");

                // Crea una nuova prenotazione
                var reservation = new StockReservation
                {
                    Id = Guid.NewGuid(),
                    OrderId = orderId,
                    ProductId = productId,
                    Quantity = quantity,
                    Status = "Reserved",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                // Riduci lo stock del prodotto
                product.QuantityInStock -= quantity;
                product.UpdatedAt = DateTime.UtcNow;

                _context.StockReservations.Add(reservation);         
                return reservation;
            }
        }

        public async Task<bool> ConfirmStockReservationAsync(Guid reservationId, CancellationToken cancellationToken = default)
        {
            var reservation = await GetStockReservationByIdAsync(reservationId, cancellationToken);
            if (reservation == null || reservation.Status != "Reserved")
                return false;

            reservation.Status = "Confirmed";
            reservation.UpdatedAt = DateTime.UtcNow;

            return true;
        }

        public async Task<bool> CancelStockReservationAsync(Guid reservationId, CancellationToken cancellationToken = default)
        {
            var reservation = await GetStockReservationByIdAsync(reservationId, cancellationToken);
            if (reservation == null)
                return false;

            // Ripristina lo stock se la prenotazione era attiva
            if (reservation.Status == "Reserved")
            {
                var product = await GetProductByIdAsync(reservation.ProductId, cancellationToken);
                if (product != null)
                {
                    product.QuantityInStock += reservation.Quantity;
                    product.UpdatedAt = DateTime.UtcNow;
                }
            }

            reservation.Status = "Cancelled";
            reservation.UpdatedAt = DateTime.UtcNow;
            return true;
        }
    }
}
