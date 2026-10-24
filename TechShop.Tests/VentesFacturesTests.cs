using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechShop.Controllers;
using TechShop.Models;
using TechShop.Models.DAO;
using TechShop.Models.ViewModels;

namespace TechShop.Tests;

/// <summary>
/// Tests de caractérisation du module Ventes / Factures : ils décrivent le comportement
/// actuel (stock, prix figé, calcul HT / TVA / timbre / TTC, numérotation, règles client),
/// pour que toute évolution du code soit vérifiée automatiquement.
/// </summary>
[Collection("Base")]
public class VentesFacturesTests(BaseDeDonnees bdd)
{
    private VenteController Ventes(TechShop.Data.AppDbContext db) =>
        BaseDeDonnees.AvecTempData(new VenteController(db, bdd.Configuration));

    private int CreerProduit(decimal prix, int stock)
    {
        using var db = bdd.NouveauContexte();
        var p = new Produit
        {
            Reference = "REF-" + Guid.NewGuid().ToString("N")[..10],
            Nom = "Produit test",
            Prix = prix,
            MarqueID = new MarqueDAO(bdd.Configuration).GetAll().First().Id,
            CategorieID = new CategorieDAO(bdd.Configuration).GetAll().First().Id
        };
        if (stock > 0) p.AjouterStock(stock);
        db.Produits.Add(p);
        db.SaveChanges();
        return p.ProduitID;
    }

    private int Stock(int produitId)
    {
        using var db = bdd.NouveauContexte();
        return db.Produits.AsNoTracking().Single(p => p.ProduitID == produitId).Quantite;
    }

    private async Task<int> CreerVente(params (int produitId, int quantite)[] lignes)
    {
        using var db = bdd.NouveauContexte();
        var vm = new VenteCreateVM { Lignes = lignes.Select(l => new VenteCreateLigneVM { ProduitID = l.produitId, Quantite = l.quantite }).ToList() };
        var result = await Ventes(db).Create(vm);
        var redirect = Assert.IsType<RedirectToActionResult>(result);
        return (int)redirect.RouteValues!["id"]!;
    }

    private async Task<IActionResult> Facturer(int venteId, TypeClient type = TypeClient.Physique, string nom = "Client test", string? matricule = null)
    {
        using var db = bdd.NouveauContexte();
        var controller = Ventes(db);
        var vm = new FacturerVM { VenteID = venteId, TypeClient = type, NomClient = nom, MatriculeFiscal = matricule };
        return await controller.Facturer(vm);
    }

    // ---------- ventes et stock ----------

    [Fact]
    public async Task UneVenteDecrementeLeStockEtFigeLePrix()
    {
        var a = CreerProduit(1200m, 10);
        var b = CreerProduit(35.5m, 4);
        var venteId = await CreerVente((a, 3), (b, 4));

        Assert.Equal(7, Stock(a));
        Assert.Equal(0, Stock(b));

        using (var db = bdd.NouveauContexte())
        {
            var p = db.Produits.Single(x => x.ProduitID == a);
            p.Prix = 9999m;                // changement de prix après la vente
            db.SaveChanges();
        }

        using var db2 = bdd.NouveauContexte();
        var vente = db2.Ventes.Include(v => v.Lignes).Single(v => v.VenteID == venteId);
        Assert.Equal(VenteStatut.EnCours, vente.Statut);
        Assert.Equal(1200m, vente.Lignes.Single(l => l.ProduitID == a).PrixUnitaire);
        Assert.Equal(3 * 1200m + 4 * 35.5m, vente.MontantHT);
    }

    [Fact]
    public async Task StockInsuffisantRefuseSansRienModifier()
    {
        var a = CreerProduit(100m, 2);
        using var db = bdd.NouveauContexte();
        var controller = Ventes(db);
        int ventesAvant = db.Ventes.Count();

        var result = await controller.Create(new VenteCreateVM { Lignes = [new() { ProduitID = a, Quantite = 3 }] });

        Assert.IsType<ViewResult>(result);
        Assert.False(controller.ModelState.IsValid);
        Assert.Equal(2, Stock(a));
        Assert.Equal(ventesAvant, bdd.NouveauContexte().Ventes.Count());
    }

    [Fact]
    public async Task UneVenteSansProduitEstRefusee()
    {
        using var db = bdd.NouveauContexte();
        var controller = Ventes(db);
        var result = await controller.Create(new VenteCreateVM { Lignes = [new() { ProduitID = 0, Quantite = 0 }] });
        Assert.IsType<ViewResult>(result);
        Assert.False(controller.ModelState.IsValid);
    }

    // ---------- factures ----------

    [Fact]
    public async Task FactureCalculeHtTvaTimbreEtTtc()
    {
        var a = CreerProduit(1250m, 5);
        var b = CreerProduit(19.99m, 10);
        var venteId = await CreerVente((a, 2), (b, 3));

        var result = await Facturer(venteId);
        Assert.IsType<RedirectToActionResult>(result);

        using var db = bdd.NouveauContexte();
        var f = db.Factures.Single(x => x.VenteID == venteId);
        decimal ht = 2 * 1250m + 3 * 19.99m;               // 2559.97
        Assert.Equal(ht, f.MontantHT);
        Assert.Equal(Math.Round(ht * 0.19m, 3), f.MontantTVA); // 486.394
        Assert.Equal(1m, f.Timbre);
        Assert.Equal(ht + Math.Round(ht * 0.19m, 3) + 1m, f.MontantTTC);
        Assert.Equal(VenteStatut.Facturee, db.Ventes.Single(v => v.VenteID == venteId).Statut);
    }

    [Fact]
    public async Task NumeroDeFactureAuFormatAnneeEtCompteur()
    {
        var a = CreerProduit(10m, 10);
        var v1 = await CreerVente((a, 1));
        var v2 = await CreerVente((a, 1));
        await Facturer(v1);
        await Facturer(v2);

        using var db = bdd.NouveauContexte();
        var n1 = db.Factures.Single(f => f.VenteID == v1).NumeroFacture;
        var n2 = db.Factures.Single(f => f.VenteID == v2).NumeroFacture;
        Assert.Matches($@"^F-{DateTime.Now.Year}-\d{{3}}$", n1);
        int c1 = int.Parse(n1[^3..]), c2 = int.Parse(n2[^3..]);
        Assert.Equal(c1 + 1, c2);
    }

    [Fact]
    public async Task ClientMoralSansMatriculeFiscalRefuse()
    {
        var a = CreerProduit(10m, 5);
        var venteId = await CreerVente((a, 1));

        var refuse = await Facturer(venteId, TypeClient.Morale, "Société Test SARL", matricule: null);
        Assert.IsType<ViewResult>(refuse);
        using (var db = bdd.NouveauContexte())
            Assert.False(db.Factures.Any(f => f.VenteID == venteId));

        var accepte = await Facturer(venteId, TypeClient.Morale, "Société Test SARL", matricule: "1234567/A/M/000");
        Assert.IsType<RedirectToActionResult>(accepte);
    }

    [Fact]
    public async Task UneVenteNePeutEtreFactureeQuUneFois()
    {
        var a = CreerProduit(10m, 5);
        var venteId = await CreerVente((a, 1));
        await Facturer(venteId);
        await Facturer(venteId);

        using var db = bdd.NouveauContexte();
        Assert.Equal(1, db.Factures.Count(f => f.VenteID == venteId));
    }

    [Fact]
    public async Task UneVenteAnnuleeNePeutPasEtreFacturee()
    {
        var a = CreerProduit(10m, 5);
        var venteId = await CreerVente((a, 1));
        using (var db = bdd.NouveauContexte())
        {
            db.Ventes.Single(v => v.VenteID == venteId).Statut = VenteStatut.Annulee;
            db.SaveChanges();
        }

        using var db2 = bdd.NouveauContexte();
        var result = await Ventes(db2).Facturer(venteId);
        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Details", redirect.ActionName);
    }

    // ---------- stock depuis la fiche produit ----------

    [Fact]
    public async Task EntreesEtSortiesDeStockDepuisLaFicheProduit()
    {
        var a = CreerProduit(10m, 0);
        using var db = bdd.NouveauContexte();
        var controller = BaseDeDonnees.AvecTempData(new ProduitController(db, null!, bdd.Configuration));

        await controller.AjouterStock(a, 8);
        await controller.RetirerStock(a, 3);
        await controller.RetirerStock(a, 50);           // refusé : stock insuffisant

        Assert.Equal(5, Stock(a));
        Assert.Equal("Stock insuffisant", controller.TempData["Error"]);
    }
}
