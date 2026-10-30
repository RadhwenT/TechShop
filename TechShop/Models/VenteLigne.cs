using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TechShop.Models
{
    public class VenteLigne
    {
        [Key]
        public int VenteLigneID { get; set; }

        [Required]
        public int VenteID { get; set; }
        public Vente? Vente { get; set; }

        [Required]
        public int ProduitID { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal PrixUnitaire { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "La quantité doit être ≥ 1")]
        public int Quantite { get; set; }

        [NotMapped]
        public Produit? Produit { get; set; }

        [NotMapped]
        public int QuantiteRetournee { get; set; }

        [NotMapped]
        public int QuantiteRetournable => Quantite - QuantiteRetournee;

        public decimal SousTotal => PrixUnitaire * Quantite;
    }
}
