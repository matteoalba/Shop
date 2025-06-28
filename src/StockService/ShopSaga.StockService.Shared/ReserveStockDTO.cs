using System;
using System.ComponentModel.DataAnnotations;

namespace ShopSaga.StockService.Shared
{
    public class ReserveStockDTO
    {
        [Required(ErrorMessage = "L'ID dell'ordine è obbligatorio")]
        public int OrderId { get; set; }

        [Required(ErrorMessage = "L'ID del prodotto è obbligatorio")]
        public Guid ProductId { get; set; }

        [Required(ErrorMessage = "La quantità è obbligatoria")]
        [Range(1, int.MaxValue, ErrorMessage = "La quantità deve essere maggiore di 0")]
        public int Quantity { get; set; }
    }
}
