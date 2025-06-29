using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ShopSaga.OrderService.Shared
{
    public class UpdateOrderItemDTO
    {
        public int Id { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }

    public class UpdateOrderDTO
    {
        public string? Status { get; set; }
        public List<UpdateOrderItemDTO>? OrderItems { get; set; }
    }
}
