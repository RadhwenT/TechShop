using System.ComponentModel.DataAnnotations;

namespace TechShop.Models
{
    public enum VenteStatut
    {
        EnAttente,      
        Validee,        
        EnExpedition,   
        Livree,        
        Annulee
    }

    public enum TypeVente
    {
        SurPlace,
        EnLigne
    }

    public class Vente
    {
        [Key]
        public int VenteID { get; set; }

        public DateTime DateVente { get; set; } = DateTime.Now;

        public VenteStatut Statut { get; set; } = VenteStatut.EnAttente;
        
        public TypeVente Type { get; set; } = TypeVente.SurPlace;

        public bool IsActive { get; set; } = true;

        public int? UtilisateurID { get; set; }
        public Utilisateur? Utilisateur { get; set; }

        public int? LivreurID { get; set; }
        public Livreur? Livreur { get; set; }
        
        public List<VenteLigne> Lignes { get; set; } = new();
        public Facture? Facture { get; set; }
        public decimal MontantHT => Lignes.Sum(l => l.SousTotal);
    }
}
