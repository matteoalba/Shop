using System;
using System.ComponentModel.DataAnnotations;

namespace ShopSaga.StockService.Shared
{
    public class UpdateProductDTO
    {
        [Required(ErrorMessage = "Il nome del prodotto è obbligatorio")]
        [MaxLength(200, ErrorMessage = "Il nome non può superare 200 caratteri")]
        public string Name { get; set; }

        [MaxLength(4000, ErrorMessage = "La descrizione non può superare 4000 caratteri")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Il prezzo è obbligatorio")]
        [Range(0, double.MaxValue, ErrorMessage = "Il prezzo deve essere maggiore o uguale a 0")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "La quantità in stock è obbligatoria")]
        [Range(0, int.MaxValue, ErrorMessage = "La quantità deve essere maggiore o uguale a 0")]
        public int QuantityInStock { get; set; }
    }
}
