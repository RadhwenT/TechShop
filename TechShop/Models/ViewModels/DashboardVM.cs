using System.Collections.Generic;

namespace TechShop.Models.ViewModels
{
    public class DashboardVM
    {
        
        public decimal ChiffreAffaireTotal { get; set; }
        public int TotalCommandesEnLigne { get; set; }
        public int TotalVentesDirectes { get; set; }
        public int TotalClients { get; set; }
        public int TotalProduits { get; set; }
        public int AlertesStock { get; set; } 

        public List<BestSellerItem> BestSellers { get; set; } = new List<BestSellerItem>();

        public List<string> ChartLabels { get; set; } = new List<string>();
        public List<decimal> ChartData { get; set; } = new List<decimal>();
       
        public Dictionary<string, int> StatusDistribution { get; set; } = new Dictionary<string, int>();
    }

    public class BestSellerItem
    {
        public string ProduitNom { get; set; } = string.Empty;
        public string Image { get; set; } = string.Empty;
        public int QuantiteVendue { get; set; }
        public decimal Revenu { get; set; }
    }
}
