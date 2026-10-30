namespace TechShop.Models
{
    public class CartItem
    {
        public int ProduitID { get; set; }
        public string Nom { get; set; } = string.Empty;
        public decimal Prix { get; set; }
        public int Quantite { get; set; }
        public string? Image { get; set; }
        public string Reference { get; set; } = string.Empty;

        public decimal SousTotal => Prix * Quantite;
    }
}
