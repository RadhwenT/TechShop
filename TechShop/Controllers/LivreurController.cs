using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechShop.Data;
using TechShop.Models;

namespace TechShop.Controllers
{
    [Authorize(Roles = "Admin")]
    public class LivreurController : Controller
    {
        private readonly AppDbContext _context;

        public LivreurController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string search)
        {
            var query = _context.Livreurs.Where(l => l.IsActive);

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(l => l.Nom.Contains(search) || l.Prenom.Contains(search) || l.Telephone.Contains(search));
            }

            ViewBag.Search = search;
            return View(await query.ToListAsync());
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var livreur = await _context.Livreurs
                .FirstOrDefaultAsync(m => m.Id == id && m.IsActive);
            
            if (livreur == null) return NotFound();

            return View(livreur);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Livreur livreur)
        {
            if (ModelState.IsValid)
            {
                _context.Add(livreur);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(livreur);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var livreur = await _context.Livreurs.FindAsync(id);
            if (livreur == null || !livreur.IsActive) return NotFound();

            return View(livreur);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Livreur livreur)
        {
            if (id != livreur.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(livreur);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Livreurs.Any(e => e.Id == livreur.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(livreur);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var livreur = await _context.Livreurs
                .FirstOrDefaultAsync(m => m.Id == id && m.IsActive);
            
            if (livreur == null) return NotFound();

            return View(livreur);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var livreur = await _context.Livreurs.FindAsync(id);
            if (livreur != null)
            {
                livreur.IsActive = false;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Deleted(string search)
        {
            var query = _context.Livreurs.Where(l => !l.IsActive);

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(l => l.Nom.Contains(search) || l.Prenom.Contains(search) || l.Telephone.Contains(search));
            }

            ViewBag.Search = search;
            return View(await query.ToListAsync());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Restore(int id)
        {
            var livreur = await _context.Livreurs.FindAsync(id);
            if (livreur != null)
            {
                livreur.IsActive = true;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Deleted));
        }
    }
}
