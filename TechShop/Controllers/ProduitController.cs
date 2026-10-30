using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TechShop.Data;
using TechShop.Models;
using TechShop.Models.DAO;

namespace TechShop.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ProduitController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly MarqueDAO _marqueDAO;
        private readonly CategorieDAO _categorieDAO;

        public ProduitController(AppDbContext context, IWebHostEnvironment env, IConfiguration configuration)
        {
            _context = context;
            _env = env;
            _marqueDAO = new MarqueDAO(configuration);
            _categorieDAO = new CategorieDAO(configuration);
        }


        private void PopulateLists(int? selectedCategorie = null, int? selectedMarque = null)
        {
            ViewBag.Categories = new SelectList(
                _categorieDAO.GetAll().OrderBy(c => c.Name),
                "Id", "Name", selectedCategorie);

            ViewBag.Marques = new SelectList(
                _marqueDAO.GetAll().OrderBy(m => m.Name),
                "Id", "Name", selectedMarque);
        }

        /// <summary>
        /// Enrichit une liste de produits avec les noms Categorie et Marque via les DAOs.
        /// Nécessaire car [NotMapped] empêche EF de faire .Include().
        /// </summary>
        private void EnrichProduits(List<Produit> produits)
        {
            var categories = _categorieDAO.GetAll();
            var marques = _marqueDAO.GetAll();

            foreach (var p in produits)
            {
                p.Categorie = categories.FirstOrDefault(c => c.Id == p.CategorieID);
                p.Marque = marques.FirstOrDefault(m => m.Id == p.MarqueID);
            }
        }

        private async Task<string?> SaveImageAsync(IFormFile? file)
        {
            if (file == null || file.Length == 0) return null;

            var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowed.Contains(ext)) return null;

            var folder = Path.Combine(_env.WebRootPath, "images", "produits");
            Directory.CreateDirectory(folder);

            var fileName = $"{Guid.NewGuid()}{ext}";
            var path = Path.Combine(folder, fileName);

            using var stream = new FileStream(path, FileMode.Create);
            await file.CopyToAsync(stream);

            return $"/images/produits/{fileName}";
        }

        private void DeleteImageFile(string? imagePath)
        {
            if (string.IsNullOrEmpty(imagePath)) return;
            var full = Path.Combine(_env.WebRootPath, imagePath.TrimStart('/'));
            if (System.IO.File.Exists(full))
                System.IO.File.Delete(full);
        }

        public async Task<IActionResult> Stock(bool alertOnly = true)
        {
            var query = _context.Produits.Where(p => p.IsActive);
            
            if (alertOnly)
            {
                query = query.Where(p => p.Quantite < 5);
            }

            var produits = await query.OrderBy(p => p.Quantite).ToListAsync();
            EnrichProduits(produits);

            ViewBag.AlertOnly = alertOnly;
            return View(produits);
        }

        public async Task<IActionResult> Index(string search, int? marqueId, int? categorieId)
        {
            var query = _context.Produits.Where(p => p.IsActive);

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(p => p.Nom.StartsWith(search) || p.Reference.StartsWith(search));

            if (marqueId.HasValue)
                query = query.Where(p => p.MarqueID == marqueId.Value);

            if (categorieId.HasValue)
                query = query.Where(p => p.CategorieID == categorieId.Value);

            var produits = await query.ToListAsync();
            EnrichProduits(produits);

            ViewBag.Search = search;
            PopulateLists(categorieId, marqueId);
            return View(produits);
        }

        public async Task<IActionResult> Details(int id)
        {
            var produit = await _context.Produits.FirstOrDefaultAsync(p => p.ProduitID == id && p.IsActive);
            if (produit == null) return NotFound();

            // Enrichir manuellement
            produit.Categorie = _categorieDAO.GetAll().FirstOrDefault(c => c.Id == produit.CategorieID);
            produit.Marque = _marqueDAO.GetAll().FirstOrDefault(m => m.Id == produit.MarqueID);

            return View(produit);
        }

        public IActionResult Create()
        {
            PopulateLists();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Produit produit, IFormFile? imageFile)
        {
            ModelState.Remove("Quantite");
            ModelState.Remove("Categorie");
            ModelState.Remove("Marque");
            ModelState.Remove("Image");

            if (ModelState.IsValid)
            {
                bool refExists = await _context.Produits
                    .AnyAsync(p => p.Reference.ToLower() == produit.Reference.ToLower());
                if (refExists)
                {
                    ModelState.AddModelError("Reference", "Cette référence existe déjà.");
                    PopulateLists(produit.CategorieID, produit.MarqueID);
                    return View(produit);
                }

                produit.Image = await SaveImageAsync(imageFile);
                _context.Add(produit);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            PopulateLists(produit.CategorieID, produit.MarqueID);
            return View(produit);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var produit = await _context.Produits.FindAsync(id);
            if (produit == null || !produit.IsActive) return NotFound();

            PopulateLists(produit.CategorieID, produit.MarqueID);
            return View(produit);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Produit produit, IFormFile? imageFile, bool removeImage = false)
        {
            ModelState.Remove("Quantite");
            ModelState.Remove("Categorie");
            ModelState.Remove("Marque");
            ModelState.Remove("Image");

            if (ModelState.IsValid)
            {
                bool refExists = await _context.Produits
                    .AnyAsync(p => p.Reference.ToLower() == produit.Reference.ToLower()
                                   && p.ProduitID != produit.ProduitID);
                if (refExists)
                {
                    ModelState.AddModelError("Reference", "Cette référence existe déjà.");
                    PopulateLists(produit.CategorieID, produit.MarqueID);
                    return View(produit);
                }

                var existing = await _context.Produits.AsNoTracking()
                    .FirstOrDefaultAsync(p => p.ProduitID == produit.ProduitID);

                if (removeImage)
                {
                    DeleteImageFile(existing?.Image);
                    produit.Image = null;
                }
                else if (imageFile != null && imageFile.Length > 0)
                {
                    DeleteImageFile(existing?.Image);
                    produit.Image = await SaveImageAsync(imageFile);
                }
                else
                {
                    produit.Image = existing?.Image;
                }

                try
                {
                    // Charger l'entité trackée pour ne modifier que les champs voulus
                    // sans jamais toucher à Quantite (private setter)
                    var tracked = await _context.Produits.FindAsync(produit.ProduitID);
                    if (tracked == null) return NotFound();

                    tracked.Reference = produit.Reference;
                    tracked.Nom = produit.Nom;
                    tracked.Description = produit.Description;
                    tracked.Prix = produit.Prix;
                    tracked.CategorieID = produit.CategorieID;
                    tracked.MarqueID = produit.MarqueID;
                    tracked.IsActive = produit.IsActive;
                    tracked.Image = produit.Image;

                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Produits.Any(p => p.ProduitID == produit.ProduitID))
                        return NotFound();
                    throw;
                }

                return RedirectToAction(nameof(Index));
            }

            PopulateLists(produit.CategorieID, produit.MarqueID);
            return View(produit);
        }

        public async Task<IActionResult> Delete(int id)
        {
            var produit = await _context.Produits
                .FirstOrDefaultAsync(p => p.ProduitID == id && p.IsActive);

            if (produit == null) return NotFound();

            // Enrichir manuellement
            produit.Categorie = _categorieDAO.GetAll().FirstOrDefault(c => c.Id == produit.CategorieID);
            produit.Marque = _marqueDAO.GetAll().FirstOrDefault(m => m.Id == produit.MarqueID);

            return View(produit);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var produit = await _context.Produits.FindAsync(id);
            if (produit != null)
            {
                produit.IsActive = false;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Deleted(string search)
        {
            var query = _context.Produits.Where(p => !p.IsActive);

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(p => p.Nom.StartsWith(search) || p.Reference.StartsWith(search));

            var produits = await query.ToListAsync();
            EnrichProduits(produits);

            ViewBag.Search = search;
            return View(produits);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Restore(int id, string search)
        {
            var produit = await _context.Produits.FindAsync(id);
            if (produit != null)
            {
                produit.IsActive = true;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Deleted), new { search });
        }

        public async Task<IActionResult> ManageStock(int id)
        {
            var produit = await _context.Produits.FindAsync(id);
            if (produit == null || !produit.IsActive) return NotFound();
            return View(produit);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AjouterStock(int id, int quantite)
        {
            var produit = await _context.Produits.FindAsync(id);
            if (produit == null) return NotFound();

            try
            {
                produit.AjouterStock(quantite);
                await _context.SaveChangesAsync();
                TempData["Success"] = $"{quantite} unité(s) ajoutée(s) au stock.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(ManageStock), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RetirerStock(int id, int quantite)
        {
            var produit = await _context.Produits.FindAsync(id);
            if (produit == null) return NotFound();

            try
            {
                produit.RetirerStock(quantite);
                await _context.SaveChangesAsync();
                TempData["Success"] = $"{quantite} unité(s) retirée(s) du stock.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(ManageStock), new { id });
        }
    }
}
