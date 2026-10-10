using System.ComponentModel.DataAnnotations;

namespace PROJET.Models
{
    public enum VenteStatut
    {
        EnCours,
        Facturee,
        Annulee
    }

    public class Vente
    {
        [Key]
        public int VenteID { get; set; }

        public DateTime DateVente { get; set; } = DateTime.Now;

        public VenteStatut Statut { get; set; } = VenteStatut.EnCours;

        public bool IsActive { get; set; } = true;

        // Navigation
        public List<VenteLigne> Lignes { get; set; } = new();
        public Facture? Facture { get; set; }
        public decimal MontantHT => Lignes.Sum(l => l.SousTotal);
    }
}
