using System.ComponentModel.DataAnnotations;

namespace TechShop.Models.ViewModels
{
    public class FacturerVM
    {
        public int VenteID { get; set; }

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
    }
}
