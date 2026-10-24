using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace TechShop.Models
{
    [Index(nameof(Reference), IsUnique = true)]
    public class Produit
    {
        [Key]
        public int ProduitID { get; set; }

        [Required(ErrorMessage = "La référence est obligatoire")]
        [StringLength(30)]
        public string Reference { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le nom est obligatoire")]
        [StringLength(50)]
        public string Nom { get; set; } = string.Empty;

        public string? Image {  get; set; }

        public string? Description { get; set; }

        public int Quantite { get; private set; } = 0;

        [Required]
        [Range(0, double.MaxValue)]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Prix { get; set; }

        [Required]
        public int CategorieID { get; set; }

        [NotMapped]
        public Categorie? Categorie { get; set; }

        [Required]
        public int MarqueID { get; set; }
        [NotMapped]
        public Marque? Marque { get; set; }

        public bool IsActive { get; set; } = true;


        public void AjouterStock(int quantite)
        {
            if (quantite <= 0)
                throw new ArgumentException("La quantité ajoutée doit être > 0");

            Quantite += quantite;
        }

        public void RetirerStock(int quantite)
        {
            if (quantite <= 0)
                throw new ArgumentException("La quantité retirée doit être > 0");

            if (quantite > Quantite)
                throw new InvalidOperationException("Stock insuffisant");

            Quantite -= quantite;
        }
    }
}
