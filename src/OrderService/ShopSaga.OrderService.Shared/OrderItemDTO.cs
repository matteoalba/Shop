using System;

namespace ShopSaga.OrderService.Shared
{
    public class OrderItemDTO
    {
        public int Id { get; set; }
        public Guid ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }
}
