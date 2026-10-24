using Microsoft.AspNetCore.Mvc;
using TechShop.Models;
using TechShop.Models.DAO;

namespace TechShop.Controllers
{
    public class CategorieController : Controller
    {
        private readonly CategorieDAO _categorieDAO;

        public CategorieController(IConfiguration configuration)
        {
            _categorieDAO = new CategorieDAO(configuration);
        }

        
        public IActionResult Index(string search)
        {
            List<Categorie> categories;

            if (string.IsNullOrWhiteSpace(search))
            {
                categories = _categorieDAO.GetAll();
            }
            else
            {
                categories = _categorieDAO.GetByName(search);
            }

            ViewBag.Search = search;
            return View(categories);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Categorie categorie)
        {
            if (ModelState.IsValid)
            {
                
                if (_categorieDAO.ExistsName(categorie.Name))
                {
                    ModelState.AddModelError("Name", "Ce nom de marque existe déjà.");
                    return View(categorie);
                }

                _categorieDAO.AddCategorie(categorie);
                return RedirectToAction(nameof(Index));
            }

            return View(categorie);
        }

        public IActionResult Edit(int id)
        {
            var categorie = _categorieDAO.GetByName("") .FirstOrDefault(m => m.Id == id);

            if (categorie == null)
                return NotFound();

            return View(categorie);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Categorie categorie)
        {
            if (ModelState.IsValid)
            {
                if (_categorieDAO.ExistsName(categorie.Name, categorie.Id))
                {
                    ModelState.AddModelError("Name", "Ce nom de marque existe déjà.");
                    return View(categorie);
                }

                _categorieDAO.UpdateCategorie(categorie);
                return RedirectToAction(nameof(Index));
            }

            return View(categorie);
        }

        public IActionResult Delete(int id)
        {
            var categorie = _categorieDAO.GetAll()
                                   .FirstOrDefault(m => m.Id == id);

            if (categorie == null)
                return NotFound();

            return View(categorie);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            _categorieDAO.DeleteCategorie(id);
            return RedirectToAction(nameof(Index));
        }

        
        public IActionResult Deleted(string search)
        {
            var categories = _categorieDAO.GetDeleted(search ?? "");
            ViewBag.Search = search;
            return View(categories);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Restore(int id, string search)
        {
            _categorieDAO.RestoreCategorie(id);
            return RedirectToAction(nameof(Deleted), new { search });
        }
    }
}