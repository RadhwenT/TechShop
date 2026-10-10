using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PROJET.Models
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

        // Numéro auto : F-AAAA-NNN
        [Required]
        [StringLength(20)]
        public string NumeroFacture { get; set; } = string.Empty;

        public DateTime DateFacture { get; set; } = DateTime.Now;

        // Relation 1-1 avec Vente
        [Required]
        public int VenteID { get; set; }
        public Vente? Vente { get; set; }

        // Client
        [Required]
        public TypeClient TypeClient { get; set; }

        /// <summary>Nom + Prénom pour client physique / Raison sociale pour client moral</summary>
        [Required(ErrorMessage = "Le nom / la raison sociale est obligatoire")]
        [StringLength(100)]
        public string NomClient { get; set; } = string.Empty;

        /// <summary>Matricule fiscal – obligatoire pour client moral</summary>
        [StringLength(20)]
        public string? MatriculeFiscal { get; set; }

        [StringLength(200)]
        public string? Adresse { get; set; }

        [StringLength(20)]
        public string? Telephone { get; set; }

        [StringLength(100)]
        [EmailAddress]
        public string? Email { get; set; }

        // Montants (calculés et stockés au moment de la facturation)
        [Column(TypeName = "decimal(18,3)")]
        public decimal MontantHT { get; set; }

        [Column(TypeName = "decimal(18,3)")]
        public decimal MontantTVA { get; set; }   // 19 %

        [Column(TypeName = "decimal(18,3)")]
        public decimal Timbre { get; set; } = 1m; // 1 DT fixe

        [Column(TypeName = "decimal(18,3)")]
        public decimal MontantTTC { get; set; }

        // Constantes
        public const decimal TauxTVA = 0.19m;
        public const decimal MontantTimbre = 1m;
    }
}
