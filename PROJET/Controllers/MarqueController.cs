using Microsoft.AspNetCore.Mvc;
using PROJET.Models;
using PROJET.Models.DAO;

namespace PROJET.Controllers
{
    public class MarqueController : Controller
    {
        private readonly MarqueDAO _marqueDAO;

        public MarqueController(IConfiguration configuration)
        {
            _marqueDAO = new MarqueDAO(configuration);
        }

        
        public IActionResult Index(string search)
        {
            List<Marque> marques;

            if (string.IsNullOrWhiteSpace(search))
            {
                marques = _marqueDAO.GetAll();
            }
            else
            {
                marques = _marqueDAO.GetByName(search);
            }

            ViewBag.Search = search; 
            return View(marques);
        }

        
        public IActionResult Create()
        {
            return View();
        }

        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Marque marque)
        {
            if (ModelState.IsValid)
            {
                
                if (_marqueDAO.ExistsName(marque.Name))
                {
                    ModelState.AddModelError("Name", "Ce nom de marque existe déjà.");
                    return View(marque);
                }

                _marqueDAO.AddMarque(marque);
                return RedirectToAction(nameof(Index));
            }

            return View(marque);
        }

       
        public IActionResult Edit(int id)
        {
            var marque = _marqueDAO.GetByName("")  
                                  .FirstOrDefault(m => m.Id == id);

            if (marque == null)
                return NotFound();

            return View(marque);
        }

        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Marque marque)
        {
            if (ModelState.IsValid)
            {
                
                if (_marqueDAO.ExistsName(marque.Name, marque.Id))
                {
                    ModelState.AddModelError("Name", "Ce nom de marque existe déjà.");
                    return View(marque);
                }

                _marqueDAO.UpdateMarque(marque);
                return RedirectToAction(nameof(Index));
            }

            return View(marque);
        }

        
        public IActionResult Delete(int id)
        {
            var marque = _marqueDAO.GetAll()
                                   .FirstOrDefault(m => m.Id == id);

            if (marque == null)
                return NotFound();

            return View(marque);
        }

        
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            _marqueDAO.DeleteMarque(id);
            return RedirectToAction(nameof(Index));
        }

        
        public IActionResult Deleted(string search)
        {
            var marques = _marqueDAO.GetDeleted(search ?? "");
            ViewBag.Search = search;
            return View(marques);
        }

        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Restore(int id, string search)
        {
            _marqueDAO.RestoreMarque(id);
            return RedirectToAction(nameof(Deleted), new { search });
        }
    }
}