using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechShop.Data;
using TechShop.Models;

namespace TechShop.Controllers
{
    [Authorize(Roles = "Admin")]
    public class CommandeController : Controller
    {
        private readonly AppDbContext _context;

        public CommandeController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(VenteStatut? statut, bool? isFacturee)
        {
            var query = _context.Ventes
                .Include(v => v.Utilisateur)
                .Include(v => v.Lignes)
                .Include(v => v.Facture)
                .Where(v => v.IsActive && v.Type == TypeVente.EnLigne);

            if (isFacturee.HasValue)
            {
                if (isFacturee.Value)
                    query = query.Where(v => v.Facture != null);
                else
                    query = query.Where(v => v.Facture == null);
            }

            if (statut.HasValue)
            {
                query = query.Where(v => v.Statut == statut.Value);
            }
            else
            {
                query = query.OrderByDescending(v => v.Statut == VenteStatut.EnAttente)
                             .ThenByDescending(v => v.DateVente);
            }

            ViewBag.Statut = statut;
            ViewBag.IsFacturee = isFacturee;
            return View(await query.ToListAsync());
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var commande = await _context.Ventes
                .Include(v => v.Utilisateur)
                .Include(v => v.Livreur)
                .Include(v => v.Lignes)
                .Include(v => v.Facture)
                .FirstOrDefaultAsync(m => m.VenteID == id && m.Type == TypeVente.EnLigne);

            if (commande == null) return NotFound();

            foreach (var ligne in commande.Lignes)
            {
                ligne.Produit = await _context.Produits.FindAsync(ligne.ProduitID);
            }

            ViewBag.Livreurs = await _context.Livreurs.Where(l => l.IsActive).ToListAsync();
            return View(commande);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Validate(int id, int livreurId)
        {
            var commande = await _context.Ventes
                .Include(v => v.Lignes)
                .FirstOrDefaultAsync(v => v.VenteID == id);

            if (commande == null) return NotFound();

            if (commande.Statut != VenteStatut.EnAttente)
            {
                TempData["Error"] = "Cette commande a déjà été traitée.";
                return RedirectToAction(nameof(Details), new { id });
            }

            try
            {
                foreach (var ligne in commande.Lignes)
                {
                    var produit = await _context.Produits.FindAsync(ligne.ProduitID);
                    if (produit == null) throw new Exception($"Produit {ligne.ProduitID} introuvable.");
                    
                    produit.RetirerStock(ligne.Quantite);
                }
            }
            catch (InvalidOperationException ex)
            {
                TempData["Error"] = "Validation impossible : " + ex.Message;
                return RedirectToAction(nameof(Details), new { id });
            }

            commande.Statut = VenteStatut.Validee;
            commande.LivreurID = livreurId;

            await _context.SaveChangesAsync();
            TempData["Success"] = "Commande validée, stock mis à jour et livreur affecté.";
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Ship(int id)
        {
            var commande = await _context.Ventes.FindAsync(id);
            if (commande == null) return NotFound();

            commande.Statut = VenteStatut.EnExpedition;
            await _context.SaveChangesAsync();
            TempData["Success"] = "La commande est maintenant en cours de livraison.";
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var commande = await _context.Ventes.FindAsync(id);
            if (commande == null) return NotFound();

            if (commande.Statut != VenteStatut.EnAttente)
            {
                TempData["Error"] = "Seules les commandes en attente peuvent être annulées.";
                return RedirectToAction(nameof(Details), new { id });
            }

            commande.Statut = VenteStatut.Annulee;
            await _context.SaveChangesAsync();
            
            TempData["Success"] = "La commande a été annulée.";
            return RedirectToAction(nameof(Details), new { id });
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Complete(int id)
        {
            var commande = await _context.Ventes.FindAsync(id);
            if (commande == null) return NotFound();

            commande.Statut = VenteStatut.Livree;
            await _context.SaveChangesAsync();
            TempData["Success"] = "La commande a été marquée comme livrée.";
            return RedirectToAction(nameof(Details), new { id });
        }
        
        // Notification count for Admin Layout
        [AllowAnonymous]
        public async Task<int> GetPendingCount()
        {
            return await _context.Ventes.CountAsync(v => v.Type == TypeVente.EnLigne && v.Statut == VenteStatut.EnAttente && v.IsActive);
        }
    }
}
