using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TechShop.Data;
using TechShop.Models.DAO;
using TechShop.Models;

namespace TechShop.Controllers
{
    public class StoreController : Controller
    {
        private readonly AppDbContext _context;
        private readonly MarqueDAO _marqueDAO;
        private readonly CategorieDAO _categorieDAO;

        public StoreController(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _marqueDAO = new MarqueDAO(configuration);
            _categorieDAO = new CategorieDAO(configuration);
        }

        public async Task<IActionResult> Index(string search, int? marqueId, int? categorieId)
        {
            var query = _context.Produits.Where(p => p.IsActive);

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(p => p.Nom.Contains(search) || p.Reference.Contains(search));

            if (marqueId.HasValue)
                query = query.Where(p => p.MarqueID == marqueId.Value);

            if (categorieId.HasValue)
                query = query.Where(p => p.CategorieID == categorieId.Value);

            var produits = await query.ToListAsync();

            var categories = _categorieDAO.GetAll();
            var marques = _marqueDAO.GetAll();

            foreach (var p in produits)
            {
                p.Categorie = categories.FirstOrDefault(c => c.Id == p.CategorieID);
                p.Marque = marques.FirstOrDefault(m => m.Id == p.MarqueID);
            }

            ViewBag.Search = search;
            ViewBag.Categories = new SelectList(categories.OrderBy(c => c.Name), "Id", "Name", categorieId);
            ViewBag.Marques = new SelectList(marques.OrderBy(m => m.Name), "Id", "Name", marqueId);

            return View(produits);
        }

        public async Task<IActionResult> Details(int id)
        {
            var produit = await _context.Produits.FirstOrDefaultAsync(p => p.ProduitID == id && p.IsActive);
            if (produit == null) return NotFound();

            produit.Categorie = _categorieDAO.GetAll().FirstOrDefault(c => c.Id == produit.CategorieID);
            produit.Marque = _marqueDAO.GetAll().FirstOrDefault(m => m.Id == produit.MarqueID);

            return View(produit);
        }

        [Microsoft.AspNetCore.Authorization.Authorize]
        public async Task<IActionResult> MesCommandes()
        {
            var userIdString = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var commandes = await _context.Ventes
                .Include(v => v.Lignes)
                .Include(v => v.Livreur)
                .Where(v => v.UtilisateurID == userId && v.Type == TypeVente.EnLigne && v.IsActive)
                .OrderByDescending(v => v.DateVente)
                .ToListAsync();

            foreach (var cmd in commandes)
            {
                foreach (var ligne in cmd.Lignes)
                {
                    ligne.Produit = await _context.Produits.FindAsync(ligne.ProduitID);
                }
            }

            return View(commandes);
        }

        [Microsoft.AspNetCore.Authorization.Authorize]
        public async Task<IActionResult> MaCommandeDetails(int id)
        {
            var userIdString = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var commande = await _context.Ventes
                .Include(v => v.Lignes)
                .Include(v => v.Livreur)
                .Include(v => v.Facture)
                .FirstOrDefaultAsync(v => v.VenteID == id && v.UtilisateurID == userId && v.IsActive);

            if (commande == null) return NotFound();

            // Manuellement enrichir les produits
            foreach (var ligne in commande.Lignes)
            {
                ligne.Produit = await _context.Produits.FindAsync(ligne.ProduitID);
            }

            return View(commande);
        }
        [Microsoft.AspNetCore.Authorization.Authorize]
        public async Task<IActionResult> AfficherFacture(int id)
        {
            var userIdString = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var facture = await _context.Factures
                .Include(f => f.Vente)
                    .ThenInclude(v => v!.Lignes)
                .FirstOrDefaultAsync(f => f.FactureID == id && f.Vente!.UtilisateurID == userId);

            if (facture == null) return NotFound();

            // Enrichir les produits
            foreach (var l in facture.Vente!.Lignes)
            {
                l.Produit = await _context.Produits.FindAsync(l.ProduitID);
            }

            return View(facture);
        }
    }
}
