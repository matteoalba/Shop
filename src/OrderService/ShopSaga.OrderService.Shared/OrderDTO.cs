using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShopSaga.OrderService.Shared
{
    public class OrderDTO
    {
        public int Id { get; set; }
        
        public Guid CustomerId { get; set; }

        public decimal TotalAmount { get; set; }

        public string Status { get; set; } = "Created";

        public DateTime CreatedAt { get; set; }
        
        public DateTime UpdatedAt { get; set; }
        
        public List<OrderItemDTO> OrderItems { get; set; } = new List<OrderItemDTO>();
    }
}