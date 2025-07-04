using System;

namespace ShopSaga.OrderService.Shared.Events
{
    public abstract class BaseEvent
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
