using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechShop.Data;
using TechShop.Models;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace TechShop.Controllers
{
    public class CartController : Controller
    {
        private readonly AppDbContext _context;

        public CartController(AppDbContext context)
        {
            _context = context;
        }

        private List<CartItem> GetCart()
        {
            return HttpContext.Session.GetJson<List<CartItem>>("Cart") ?? new List<CartItem>();
        }

        private void SaveCart(List<CartItem> cart)
        {
            HttpContext.Session.SetJson("Cart", cart);
        }

        /// <summary>
        /// Remet chaque ligne du panier au prix actuel du catalogue (le panier est en session et peut
        /// dater d'avant un changement de prix) ; renvoie un message par prix modifié.
        /// </summary>
        private List<string> ActualiserPrix(List<CartItem> cart)
        {
            var changements = new List<string>();
            foreach (var item in cart)
            {
                var produit = _context.Produits.Find(item.ProduitID);
                if (produit != null && produit.Prix != item.Prix)
                {
                    changements.Add($"Le prix de « {item.Nom} » est passé de {item.Prix:N2} DT à {produit.Prix:N2} DT.");
                    item.Prix = produit.Prix;
                }
            }
            if (changements.Any()) SaveCart(cart);
            return changements;
        }

        public IActionResult Index()
        {
            var cart = GetCart();
            ViewBag.PrixModifies = ActualiserPrix(cart);
            return View(cart);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(int produitId, int quantite)
        {
            var produit = await _context.Produits.FindAsync(produitId);
            if (produit == null || !produit.IsActive || quantite < 1 || produit.Quantite < quantite)
            {
                TempData["Error"] = "Produit indisponible ou quantité insuffisante.";
                return RedirectToAction("Details", "Store", new { id = produitId });
            }

            var cart = GetCart();
            var existingItem = cart.FirstOrDefault(c => c.ProduitID == produitId);

            if (existingItem != null)
            {
                if (existingItem.Quantite + quantite <= produit.Quantite)
                {
                    existingItem.Quantite += quantite;
                }
                else
                {
                    TempData["Error"] = "La quantité demandée dépasse le stock disponible.";
                    return RedirectToAction("Index");
                }
            }
            else
            {
                cart.Add(new CartItem
                {
                    ProduitID = produit.ProduitID,
                    Nom = produit.Nom,
                    Reference = produit.Reference,
                    Prix = produit.Prix,
                    Quantite = quantite,
                    Image = produit.Image
                });
            }

            SaveCart(cart);
            TempData["Success"] = "Produit ajouté au panier.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Update(int produitId, int quantite)
        {
            var cart = GetCart();
            var item = cart.FirstOrDefault(c => c.ProduitID == produitId);

            if (item != null)
            {
                var produit = _context.Produits.Find(produitId);
                if (produit != null && quantite > 0 && quantite <= produit.Quantite)
                {
                    item.Quantite = quantite;
                }
            }

            SaveCart(cart);
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Remove(int produitId)
        {
            var cart = GetCart();
            cart.RemoveAll(c => c.ProduitID == produitId);
            SaveCart(cart);
            return RedirectToAction("Index");
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout()
        {
            var cart = GetCart();
            if (!cart.Any())
            {
                TempData["Error"] = "Votre panier est vide.";
                return RedirectToAction("Index");
            }

            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int userId))
            {
                return RedirectToAction("Login", "Account");
            }

            // La commande se fait au prix actuel : si un prix a changé depuis l'ajout au panier,
            // le client est prévenu et doit valider à nouveau
            var prixModifies = ActualiserPrix(cart);
            if (prixModifies.Any())
            {
                TempData["Error"] = string.Join(" ", prixModifies) + " Vérifiez votre panier puis validez à nouveau la commande.";
                return RedirectToAction("Index");
            }

            // Create Vente
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var vente = new Vente
                {
                    DateVente = DateTime.Now,
                    Statut = VenteStatut.EnAttente,
                    Type = TypeVente.EnLigne,
                    UtilisateurID = userId,
                    IsActive = true
                };

                _context.Ventes.Add(vente);
                await _context.SaveChangesAsync();

                foreach (var item in cart)
                {
                    var produit = await _context.Produits.FindAsync(item.ProduitID);
                    if (produit == null || !produit.IsActive)
                    {
                        throw new Exception($"« {item.Nom} » n'est plus disponible : retirez-le du panier.");
                    }
                    if (produit.Quantite < item.Quantite)
                    {
                        throw new Exception($"Stock insuffisant pour {item.Nom}.");
                    }

                    var ligne = new VenteLigne
                    {
                        VenteID = vente.VenteID,
                        ProduitID = produit.ProduitID,
                        Quantite = item.Quantite,
                        PrixUnitaire = produit.Prix
                    };
                    _context.VenteLignes.Add(ligne);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                HttpContext.Session.Remove("Cart");
                TempData["Success"] = "Votre commande a été validée avec succès !";
                return RedirectToAction("Index", "Store");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                TempData["Error"] = ex.Message;
                return RedirectToAction("Index");
            }
        }
    }
}
