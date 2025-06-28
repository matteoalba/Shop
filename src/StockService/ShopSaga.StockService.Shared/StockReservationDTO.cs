using System;
using System.ComponentModel.DataAnnotations;

namespace ShopSaga.StockService.Shared
{
    public class StockReservationDTO
    {
        public Guid Id { get; set; }
        public int OrderId { get; set; }
        public Guid ProductId { get; set; }
        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
