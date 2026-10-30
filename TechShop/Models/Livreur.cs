using System.ComponentModel.DataAnnotations;

namespace TechShop.Models
{
    public class Livreur
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Le nom est obligatoire")]
        [StringLength(50)]
        public string Nom { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le prénom est obligatoire")]
        [StringLength(50)]
        public string Prenom { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le numéro de téléphone est obligatoire")]
        [Phone(ErrorMessage = "Format de téléphone invalide")]
        [StringLength(20)]
        public string Telephone { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public string NomComplet => $"{Prenom} {Nom}";
    }
}
