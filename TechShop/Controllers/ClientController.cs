using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechShop.Data;
using TechShop.Models;

namespace TechShop.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ClientController : Controller
    {
        private readonly AppDbContext _context;

        public ClientController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string search)
        {
            var query = _context.Utilisateurs
                .Where(u => u.Role == "Client")
                .OrderBy(u => u.Nom)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(u => u.Nom.Contains(search) || u.Email.Contains(search) || u.Telephone.Contains(search));
            }

            ViewBag.Search = search;
            return View(await query.ToListAsync());
        }

        
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var client = await _context.Utilisateurs
                .FirstOrDefaultAsync(m => m.Id == id && m.Role == "Client");

            if (client == null) return NotFound();

            // Historique des commandes du client
            ViewBag.Commandes = await _context.Ventes
                .Where(v => v.UtilisateurID == id && v.Type == TypeVente.EnLigne)
                .OrderByDescending(v => v.DateVente)
                .Take(10)
                .ToListAsync();

            return View(client);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleBlock(int id)
        {
            var user = await _context.Utilisateurs.FindAsync(id);
            if (user == null || user.Role == "Admin") return NotFound();

            user.IsBlocked = !user.IsBlocked;
            await _context.SaveChangesAsync();

            TempData["Success"] = user.IsBlocked ? $"Le client {user.Nom} a été bloqué." : $"Le client {user.Nom} a été débloqué.";
            
            return RedirectToAction(nameof(Index));
        }
    }
}
