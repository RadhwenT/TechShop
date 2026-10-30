using System.ComponentModel.DataAnnotations;

namespace TechShop.Models
{
    public class Utilisateur
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Le nom est obligatoire")]
        [StringLength(100)]
        public string Nom { get; set; } = string.Empty;

        [Required(ErrorMessage = "L'email est obligatoire")]
        [EmailAddress(ErrorMessage = "Format d'email invalide")]
        [StringLength(150)]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le mot de passe est obligatoire")]
        public string MotDePasse { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string Role { get; set; } = "Client"; 

        [Phone(ErrorMessage = "Format de téléphone invalide")]
        [StringLength(20)]
        public string? Telephone { get; set; }

        [StringLength(255)]
        public string? Adresse { get; set; }

        public bool IsBlocked { get; set; } = false;
    }
}
