using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TechShop.Models
{
    public enum TypeClient
    {
        Physique,
        Morale
    }

    public class Facture
    {
        [Key]
        public int FactureID { get; set; }

        [Required]
        [StringLength(20)]
        public string NumeroFacture { get; set; } = string.Empty;

        public DateTime DateFacture { get; set; } = DateTime.Now;

        [Required]
        public int VenteID { get; set; }
        public Vente? Vente { get; set; }

        [Required]
        public TypeClient TypeClient { get; set; }

        [Required(ErrorMessage = "Le nom / la raison sociale est obligatoire")]
        [StringLength(100)]
        public string NomClient { get; set; } = string.Empty;

        [StringLength(20)]
        public string? MatriculeFiscal { get; set; }

        [StringLength(200)]
        public string? Adresse { get; set; }

        [StringLength(20)]
        public string? Telephone { get; set; }

        [StringLength(100)]
        [EmailAddress]
        public string? Email { get; set; }

        [Column(TypeName = "decimal(18,3)")]
        public decimal MontantHT { get; set; }

        [Column(TypeName = "decimal(18,3)")]
        public decimal MontantTVA { get; set; }   

        [Column(TypeName = "decimal(18,3)")]
        public decimal Timbre { get; set; } = 1m; 

        [Column(TypeName = "decimal(18,3)")]
        public decimal MontantTTC { get; set; }

        public const decimal TauxTVA = 0.19m;
        public const decimal MontantTimbre = 1m;
    }
}
