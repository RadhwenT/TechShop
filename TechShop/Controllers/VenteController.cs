using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechShop.Data;
using TechShop.Models;
using TechShop.Models.DAO;
using TechShop.Models.ViewModels;

namespace TechShop.Controllers
{
    [Authorize(Roles = "Admin")]
    public class VenteController : Controller
    {
        private readonly AppDbContext _context;
        private readonly MarqueDAO _marqueDAO;
        private readonly CategorieDAO _categorieDAO;

        public VenteController(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _marqueDAO = new MarqueDAO(configuration);
            _categorieDAO = new CategorieDAO(configuration);
        }

        // ── Helpers ─────────────────────────────────────────────────────────

        /// <summary>
        /// Enrichit une liste de VenteLignes avec les objets Produit (via DAO)
        /// et calcule la quantité déjà retournée pour chaque ligne.
        /// </summary>
        private void EnrichLignes(List<VenteLigne> lignes)
        {
            foreach (var l in lignes)
            {
                l.Produit = _context.Produits.Find(l.ProduitID);
            }
        }

        private string GenererNumeroFacture()
        {
            int annee = DateTime.Now.Year;
            int count = _context.Factures.Count(f => f.DateFacture.Year == annee) + 1;
            return CalculFacture.Numero(annee, count);
        }

        // ── INDEX ────────────────────────────────────────────────────────────
        public async Task<IActionResult> Index(string? search, int? annee, int? mois, int? jour, TechShop.Models.VenteStatut? statut, bool? isFacturee)
        {
            var query = _context.Ventes
                .Include(v => v.Lignes)
                .Include(v => v.Facture)
                .Where(v => v.IsActive && v.Type == TypeVente.SurPlace)
                .OrderByDescending(v => v.DateVente)
                .AsQueryable();

            if (isFacturee.HasValue)
            {
                if (isFacturee.Value)
                    query = query.Where(v => v.Facture != null);
                else
                    query = query.Where(v => v.Facture == null);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(v =>
                    (v.Facture != null && v.Facture.NumeroFacture.Contains(search))
                    || (v.Facture != null && v.Facture.NomClient.Contains(search))
                    || v.VenteID.ToString() == search);
            }

            if (statut.HasValue)
                query = query.Where(v => v.Statut == statut.Value);

            if (annee.HasValue)
                query = query.Where(v => v.DateVente.Year == annee.Value);

            if (mois.HasValue)
                query = query.Where(v => v.DateVente.Month == mois.Value);

            if (jour.HasValue)
                query = query.Where(v => v.DateVente.Day == jour.Value);

            var ventes = await query.ToListAsync();

            // Listes pour les dropdowns de filtre
            ViewBag.Search = search;
            ViewBag.Statut = statut;
            ViewBag.IsFacturee = isFacturee;
            ViewBag.Annee = annee;
            ViewBag.Mois = mois;
            ViewBag.Jour = jour;

            // Années disponibles dans les ventes (pour le dropdown)
            ViewBag.Annees = await _context.Ventes
                .Where(v => v.IsActive && v.Type == TypeVente.SurPlace)
                .Select(v => v.DateVente.Year)
                .Distinct()
                .OrderByDescending(y => y)
                .ToListAsync();

            return View(ventes);
        }

        // ── CREATE GET ───────────────────────────────────────────────────────
        public IActionResult Create()
        {
            // Seuls les produits en stock peuvent être vendus
            var produits = _context.Produits
                .Where(p => p.IsActive && p.Quantite > 0)
                .OrderBy(p => p.Nom)
                .ToList();

            ViewBag.Produits = produits;
            return View();
        }

        // ── CREATE POST ──────────────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(VenteCreateVM vm)
        {
            // Filtrer les lignes avec une quantité > 0
            vm.Lignes = vm.Lignes.Where(l => l.ProduitID > 0 && l.Quantite > 0).ToList();

            if (!vm.Lignes.Any())
            {
                ModelState.AddModelError("", "Veuillez ajouter au moins un produit.");
            }

            // Vérifier le stock pour chaque ligne
            var produitsMap = new Dictionary<int, Produit>();
            foreach (var ligne in vm.Lignes)
            {
                var produit = await _context.Produits.FindAsync(ligne.ProduitID);
                if (produit == null || !produit.IsActive)
                {
                    ModelState.AddModelError("", $"Produit introuvable (ID: {ligne.ProduitID}).");
                    continue;
                }
                if (ligne.Quantite > produit.Quantite)
                {
                    ModelState.AddModelError("",
                        $"Stock insuffisant pour « {produit.Nom} » : {produit.Quantite} disponible(s), {ligne.Quantite} demandé(s).");
                }
                produitsMap[ligne.ProduitID] = produit;
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Produits = _context.Produits.Where(p => p.IsActive && p.Quantite > 0).OrderBy(p => p.Nom).ToList();
                return View(vm);
            }

            // Créer la vente
            var vente = new Vente { 
                DateVente = DateTime.Now,
                Statut = VenteStatut.Validee,
                Type = TypeVente.SurPlace
            };

            foreach (var ligne in vm.Lignes)
            {
                var produit = produitsMap[ligne.ProduitID];
                vente.Lignes.Add(new VenteLigne
                {
                    ProduitID = produit.ProduitID,
                    PrixUnitaire = produit.Prix,
                    Quantite = ligne.Quantite
                });
                // Décrémenter le stock
                produit.RetirerStock(ligne.Quantite);
            }

            _context.Ventes.Add(vente);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Vente #{vente.VenteID} créée avec succès.";
            return RedirectToAction(nameof(Details), new { id = vente.VenteID });
        }

        // ── DETAILS ──────────────────────────────────────────────────────────
        public async Task<IActionResult> Details(int id)
        {
            var vente = await _context.Ventes
                .Include(v => v.Lignes)
                .Include(v => v.Facture)
                .FirstOrDefaultAsync(v => v.VenteID == id);

            if (vente == null) return NotFound();

            EnrichLignes(vente.Lignes);
            return View(vente);
        }

        // ── FACTURER GET ─────────────────────────────────────────────────────
        public async Task<IActionResult> Facturer(int id)
        {
            var vente = await _context.Ventes
                .Include(v => v.Lignes)
                .Include(v => v.Facture)
                .Include(v => v.Utilisateur)
                .FirstOrDefaultAsync(v => v.VenteID == id);

            if (vente == null) return NotFound();
            if (vente.Facture != null)
            {
                TempData["Error"] = "Cette vente est déjà facturée.";
                return RedirectToAction(nameof(Details), new { id });
            }
            if (vente.Statut == VenteStatut.Annulee)
            {
                TempData["Error"] = "Impossible de facturer une vente annulée.";
                return RedirectToAction(nameof(Details), new { id });
            }

            EnrichLignes(vente.Lignes);
            ViewBag.Vente = vente;

            var vm = new FacturerVM { VenteID = id };
            
            // Pre-fill if it's an online order
            if (vente.Type == TypeVente.EnLigne && vente.Utilisateur != null)
            {
                vm.NomClient = vente.Utilisateur.Nom;
                vm.Email = vente.Utilisateur.Email;
                vm.Telephone = vente.Utilisateur.Telephone;
                vm.Adresse = vente.Utilisateur.Adresse;
                vm.TypeClient = TypeClient.Physique; // Default for users
            }

            return View(vm);
        }

        // ── FACTURER POST ────────────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Facturer(FacturerVM vm)
        {
            // Règle : MatriculeFiscal obligatoire pour personne morale
            if (vm.TypeClient == TypeClient.Morale && string.IsNullOrWhiteSpace(vm.MatriculeFiscal))
            {
                ModelState.AddModelError("MatriculeFiscal", "Le matricule fiscal est obligatoire pour un client moral.");
            }

            var vente = await _context.Ventes
                .Include(v => v.Lignes)
                .Include(v => v.Facture)
                .FirstOrDefaultAsync(v => v.VenteID == vm.VenteID);

            if (vente == null) return NotFound();
            if (vente.Facture != null)
            {
                TempData["Error"] = "Cette vente est déjà facturée.";
                return RedirectToAction(nameof(Details), new { id = vm.VenteID });
            }

            if (!ModelState.IsValid)
            {
                EnrichLignes(vente.Lignes);
                ViewBag.Vente = vente;
                return View(vm);
            }

            // Calcul des montants (le prix unitaire est déjà TTC ; règles dans CalculFacture)
            var montants = CalculFacture.Calculer(vente.Lignes);

            var facture = new Facture
            {
                VenteID = vente.VenteID,
                NumeroFacture = GenererNumeroFacture(),
                DateFacture = DateTime.Now,
                TypeClient = vm.TypeClient,
                NomClient = vm.NomClient,
                MatriculeFiscal = vm.MatriculeFiscal,
                Adresse = vm.Adresse,
                Telephone = vm.Telephone,
                Email = vm.Email,
                MontantHT = montants.HT,
                MontantTVA = montants.TVA,
                Timbre = montants.Timbre,
                MontantTTC = montants.TTC
            };

            _context.Factures.Add(facture);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Facture {facture.NumeroFacture} générée avec succès.";
            return RedirectToAction(nameof(AfficherFacture), new { id = facture.FactureID });
        }

        // ── AFFICHER FACTURE ─────────────────────────────────────────────────
        public async Task<IActionResult> AfficherFacture(int id)
        {
            var facture = await _context.Factures
                .Include(f => f.Vente)
                    .ThenInclude(v => v!.Lignes)
                .FirstOrDefaultAsync(f => f.FactureID == id);

            if (facture == null) return NotFound();

            EnrichLignes(facture.Vente!.Lignes);
            return View(facture);
        }

    }
}
