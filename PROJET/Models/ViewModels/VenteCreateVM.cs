namespace PROJET.Models.ViewModels
{
    public class VenteCreateLigneVM
    {
        public int ProduitID { get; set; }
        public int Quantite { get; set; }
    }

    public class VenteCreateVM
    {
        public List<VenteCreateLigneVM> Lignes { get; set; } = new();
    }
}
