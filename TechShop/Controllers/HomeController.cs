using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechShop.Data;
using TechShop.Models;
using TechShop.Models.ViewModels;

namespace TechShop.Controllers
{
    [Authorize(Roles = "Admin")]
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;

        public HomeController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var model = new DashboardVM();

            // 1. Chiffres clés
            model.ChiffreAffaireTotal = await _context.Ventes
                .Where(v => v.IsActive && v.Statut != VenteStatut.Annulee)
                .SumAsync(v => v.Lignes.Sum(l => l.PrixUnitaire * l.Quantite));

            model.TotalCommandesEnLigne = await _context.Ventes.CountAsync(v => v.Type == TypeVente.EnLigne && v.IsActive);
            model.TotalVentesDirectes = await _context.Ventes.CountAsync(v => v.Type == TypeVente.SurPlace && v.IsActive);
            model.TotalClients = await _context.Utilisateurs.CountAsync(u => u.Role == "Client");
            model.TotalProduits = await _context.Produits.CountAsync(p => p.IsActive);
            model.AlertesStock = await _context.Produits.CountAsync(p => p.IsActive && p.Quantite < 5);

            // 2. Meilleures ventes
            var topLignes = await _context.VenteLignes
                .Include(l => l.Vente)
                .Where(l => l.Vente!.IsActive && l.Vente.Statut != VenteStatut.Annulee)
                .GroupBy(l => l.ProduitID)
                .Select(g => new
                {
                    ProduitID = g.Key,
                    TotalQty = g.Sum(x => x.Quantite),
                    TotalRevenue = g.Sum(x => x.Quantite * x.PrixUnitaire)
                })
                .OrderByDescending(x => x.TotalQty)
                .Take(5)
                .ToListAsync();

            foreach (var item in topLignes)
            {
                var p = await _context.Produits.FindAsync(item.ProduitID);
                if (p != null)
                {
                    model.BestSellers.Add(new BestSellerItem
                    {
                        ProduitNom = p.Nom,
                        Image = p.Image ?? "",
                        QuantiteVendue = item.TotalQty,
                        Revenu = item.TotalRevenue
                    });
                }
            }

            // 3. Données graphiques (Revenu des 7 derniers jours)
            for (int i = 6; i >= 0; i--)
            {
                var date = DateTime.Today.AddDays(-i);
                model.ChartLabels.Add(date.ToString("dd/MM"));
                
                var dailyRevenue = await _context.Ventes
                    .Where(v => v.DateVente.Date == date && v.IsActive && v.Statut != VenteStatut.Annulee)
                    .SumAsync(v => v.Lignes.Sum(l => l.PrixUnitaire * l.Quantite));
                
                model.ChartData.Add(dailyRevenue);
            }

            // 4. Distribution des statuts
            var stats = await _context.Ventes
                .Where(v => v.Type == TypeVente.EnLigne && v.IsActive)
                .GroupBy(v => v.Statut)
                .Select(g => new { Statut = g.Key.ToString(), Count = g.Count() })
                .ToListAsync();

            foreach (var s in stats)
            {
                model.StatusDistribution[s.Statut] = s.Count;
            }

            return View(model);
        }
    }
}
