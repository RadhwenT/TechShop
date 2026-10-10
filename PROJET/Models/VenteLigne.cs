using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PROJET.Models
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

        // Snapshot du prix au moment de la vente (découplé du catalogue)
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal PrixUnitaire { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "La quantité doit être ≥ 1")]
        public int Quantite { get; set; }

        // Navigation (NotMapped car Produit est géré via DAO, pas EF)
        [NotMapped]
        public Produit? Produit { get; set; }

        // Quantité déjà retournée (calculée depuis les RetourLignes)
        [NotMapped]
        public int QuantiteRetournee { get; set; }

        [NotMapped]
        public int QuantiteRetournable => Quantite - QuantiteRetournee;

        public decimal SousTotal => PrixUnitaire * Quantite;
    }
}
