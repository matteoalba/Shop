using System;
using System.Collections.Generic;

namespace ShopSaga.OrderService.Shared.Events
{
    public class OrderCreatedEvent : BaseEvent
    {
        public int OrderId { get; set; }
        public Guid CustomerId { get; set; }
        public decimal TotalAmount { get; set; }
        public List<OrderItemEvent> Items { get; set; } = new List<OrderItemEvent>();
    }

    public class OrderItemEvent
    {
        public Guid ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
    }
}
