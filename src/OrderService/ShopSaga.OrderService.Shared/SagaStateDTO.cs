using System;

namespace ShopSaga.OrderService.Shared
{
    public class SagaStateDTO
    {
        public int OrderId { get; set; }
        
        public string Status { get; set; }
        
        public string PaymentStatus { get; set; }
        
        public string StockStatus { get; set; }
        
        public DateTime CreatedAt { get; set; }
        
        public DateTime UpdatedAt { get; set; }
    }
}
