using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechShop.Controllers;
using TechShop.Models;
using TechShop.Models.ViewModels;

namespace TechShop.Tests;

/// <summary>
/// Parcours client de la boutique en ligne : panier en session, commande, traitement de la
/// commande par l'administrateur (validation, livreur, expédition, livraison, annulation),
/// et cloisonnement des commandes et factures entre clients.
/// </summary>
[Collection("Base")]
public class BoutiqueEnLigneTests(BaseDeDonnees bdd)
{
    private List<CartItem> Panier(Requete r) => r.Session.GetJson<List<CartItem>>("Cart") ?? [];

    /// <summary>Un client connecté ajoute les produits au panier puis passe commande ; renvoie l'id de la commande.</summary>
    private async Task<int> Commander(Utilisateur client, params (int produitId, int quantite)[] lignes)
    {
        var r = new Requete().Connecte(client);
        using var db = bdd.NouveauContexte();
        foreach (var (produitId, quantite) in lignes)
            await r.Pour(new CartController(db)).Add(produitId, quantite);
        await r.Pour(new CartController(db)).Checkout();

        using var db2 = bdd.NouveauContexte();
        return db2.Ventes.Where(v => v.UtilisateurID == client.Id).OrderByDescending(v => v.VenteID).First().VenteID;
    }

    private Vente Commande(int id)
    {
        using var db = bdd.NouveauContexte();
        return db.Ventes.Include(v => v.Lignes).Single(v => v.VenteID == id);
    }

    private CommandeController Admin(TechShop.Data.AppDbContext db) =>
        new Requete().Connecte(Donnees.Utilisateur(bdd, "Admin")).Pour(new CommandeController(db));

    // ---------- panier ----------

    [Fact]
    public async Task AjouterModifierEtRetirerDuPanier()
    {
        var a = Donnees.Produit(bdd, 899m, 10);
        var b = Donnees.Produit(bdd, 49.9m, 10);
        var r = new Requete();                     // visiteur non connecté : le panier fonctionne déjà
        using var db = bdd.NouveauContexte();

        await r.Pour(new CartController(db)).Add(a, 2);
        await r.Pour(new CartController(db)).Add(a, 1);
        await r.Pour(new CartController(db)).Add(b, 1);
        Assert.Equal(3, Panier(r).Single(i => i.ProduitID == a).Quantite);
        Assert.Equal(3 * 899m + 49.9m, Panier(r).Sum(i => i.SousTotal));

        r.Pour(new CartController(db)).Update(a, 5);
        Assert.Equal(5, Panier(r).Single(i => i.ProduitID == a).Quantite);

        r.Pour(new CartController(db)).Remove(b);
        Assert.Single(Panier(r));
    }

    [Fact]
    public async Task LePanierNeDepassePasLeStock()
    {
        var a = Donnees.Produit(bdd, 100m, 3);
        var r = new Requete();
        using var db = bdd.NouveauContexte();

        var cart = r.Pour(new CartController(db));
        await cart.Add(a, 4);                      // plus que le stock
        Assert.Empty(Panier(r));
        Assert.NotNull(cart.TempData["Error"]);

        await r.Pour(new CartController(db)).Add(a, 2);
        await r.Pour(new CartController(db)).Add(a, 2);   // 2 + 2 > 3 : refusé
        Assert.Equal(2, Panier(r).Single().Quantite);

        r.Pour(new CartController(db)).Update(a, 10);     // modification au-delà du stock ignorée
        Assert.Equal(2, Panier(r).Single().Quantite);
    }

    // ---------- commande ----------

    [Fact]
    public async Task UneCommandeEnLigneEstEnAttenteEtLePanierEstVide()
    {
        var a = Donnees.Produit(bdd, 1190m, 5);
        var client = Donnees.Utilisateur(bdd);
        var r = new Requete().Connecte(client);
        using var db = bdd.NouveauContexte();

        await r.Pour(new CartController(db)).Add(a, 2);
        var result = await r.Pour(new CartController(db)).Checkout();

        Assert.IsType<RedirectToActionResult>(result);
        Assert.Empty(Panier(r));
        using var db2 = bdd.NouveauContexte();
        var vente = db2.Ventes.Include(v => v.Lignes).Single(v => v.UtilisateurID == client.Id);
        Assert.Equal(TypeVente.EnLigne, vente.Type);
        Assert.Equal(VenteStatut.EnAttente, vente.Statut);
        Assert.Equal(2, vente.Lignes.Single().Quantite);
        Assert.Equal(1190m, vente.Lignes.Single().PrixUnitaire);
        Assert.Equal(5, Donnees.Stock(bdd, a));   // le stock ne bouge qu'à la validation
    }

    [Fact]
    public async Task CommanderAvecUnPanierVideNeCreeRien()
    {
        var client = Donnees.Utilisateur(bdd);
        using var db = bdd.NouveauContexte();
        await new Requete().Connecte(client).Pour(new CartController(db)).Checkout();
        Assert.False(bdd.NouveauContexte().Ventes.Any(v => v.UtilisateurID == client.Id));
    }

    // ---------- traitement par l'administrateur ----------

    [Fact]
    public async Task ValiderUneCommandeRetireLeStockEtAffecteUnLivreur()
    {
        var a = Donnees.Produit(bdd, 500m, 6);
        var id = await Commander(Donnees.Utilisateur(bdd), (a, 4));
        var livreur = Donnees.Livreur(bdd);

        using (var db = bdd.NouveauContexte())
            await Admin(db).Validate(id, livreur);

        var c = Commande(id);
        Assert.Equal(VenteStatut.Validee, c.Statut);
        Assert.Equal(livreur, c.LivreurID);
        Assert.Equal(2, Donnees.Stock(bdd, a));

        using (var db = bdd.NouveauContexte())
            await Admin(db).Validate(id, livreur);   // déjà traitée : sans effet
        Assert.Equal(2, Donnees.Stock(bdd, a));
    }

    [Fact]
    public async Task ValidationRefuseeSiLeStockAEteVenduEntretemps()
    {
        var a = Donnees.Produit(bdd, 500m, 3);
        var id = await Commander(Donnees.Utilisateur(bdd), (a, 3));
        using (var db = bdd.NouveauContexte())
        {
            db.Produits.Single(p => p.ProduitID == a).RetirerStock(2);   // vente en magasin entre-temps
            db.SaveChanges();
        }

        using (var db = bdd.NouveauContexte())
        {
            var admin = Admin(db);
            await admin.Validate(id, Donnees.Livreur(bdd));
            Assert.NotNull(admin.TempData["Error"]);
        }
        Assert.Equal(VenteStatut.EnAttente, Commande(id).Statut);
        Assert.Equal(1, Donnees.Stock(bdd, a));
    }

    [Fact]
    public async Task CycleDeVieExpeditionPuisLivraison()
    {
        var a = Donnees.Produit(bdd, 100m, 5);
        var id = await Commander(Donnees.Utilisateur(bdd), (a, 1));
        using (var db = bdd.NouveauContexte()) await Admin(db).Validate(id, Donnees.Livreur(bdd));
        using (var db = bdd.NouveauContexte()) await Admin(db).Ship(id);
        Assert.Equal(VenteStatut.EnExpedition, Commande(id).Statut);
        using (var db = bdd.NouveauContexte()) await Admin(db).Complete(id);
        Assert.Equal(VenteStatut.Livree, Commande(id).Statut);
    }

    [Fact]
    public async Task SeuleUneCommandeEnAttentePeutEtreAnnulee()
    {
        var a = Donnees.Produit(bdd, 100m, 5);
        var enAttente = await Commander(Donnees.Utilisateur(bdd), (a, 1));
        using (var db = bdd.NouveauContexte()) await Admin(db).Cancel(enAttente);
        Assert.Equal(VenteStatut.Annulee, Commande(enAttente).Statut);

        var validee = await Commander(Donnees.Utilisateur(bdd), (a, 1));
        using (var db = bdd.NouveauContexte()) await Admin(db).Validate(validee, Donnees.Livreur(bdd));
        using (var db = bdd.NouveauContexte()) await Admin(db).Cancel(validee);
        Assert.Equal(VenteStatut.Validee, Commande(validee).Statut);
    }

    [Fact]
    public async Task LaFactureDUneCommandeEnLigneReprendLesCoordonneesDuClient()
    {
        var a = Donnees.Produit(bdd, 100m, 5);
        var client = Donnees.Utilisateur(bdd);
        var id = await Commander(client, (a, 1));

        using var db = bdd.NouveauContexte();
        var ventes = new Requete().Connecte(Donnees.Utilisateur(bdd, "Admin")).Pour(new VenteController(db, bdd.Configuration));
        var vue = Assert.IsType<ViewResult>(await ventes.Facturer(id));
        var vm = Assert.IsType<FacturerVM>(vue.Model);
        Assert.Equal(client.Nom, vm.NomClient);
        Assert.Equal(client.Email, vm.Email);
        Assert.Equal(TypeClient.Physique, vm.TypeClient);
    }

    // ---------- cloisonnement entre clients ----------

    [Fact]
    public async Task UnClientNeVoitQueSesPropresCommandesEtFactures()
    {
        var a = Donnees.Produit(bdd, 100m, 10);
        var alice = Donnees.Utilisateur(bdd);
        var bob = Donnees.Utilisateur(bdd);
        var commandeAlice = await Commander(alice, (a, 1));
        await Commander(bob, (a, 1));

        int factureAlice;
        using (var db = bdd.NouveauContexte())
        {
            var ventes = new Requete().Connecte(Donnees.Utilisateur(bdd, "Admin")).Pour(new VenteController(db, bdd.Configuration));
            await ventes.Facturer(new FacturerVM { VenteID = commandeAlice, TypeClient = TypeClient.Physique, NomClient = alice.Nom });
            factureAlice = db.Factures.Single(f => f.VenteID == commandeAlice).FactureID;
        }

        using var db2 = bdd.NouveauContexte();
        var boutiqueBob = new Requete().Connecte(bob);
        Assert.IsType<NotFoundResult>(await boutiqueBob.Pour(new StoreController(db2, bdd.Configuration)).MaCommandeDetails(commandeAlice));
        Assert.IsType<NotFoundResult>(await boutiqueBob.Pour(new StoreController(db2, bdd.Configuration)).AfficherFacture(factureAlice));

        var mesCommandes = Assert.IsType<ViewResult>(await boutiqueBob.Pour(new StoreController(db2, bdd.Configuration)).MesCommandes());
        Assert.All(Assert.IsAssignableFrom<IEnumerable<Vente>>(mesCommandes.Model), v => Assert.Equal(bob.Id, v.UtilisateurID));

        var boutiqueAlice = new Requete().Connecte(alice);
        Assert.IsType<ViewResult>(await boutiqueAlice.Pour(new StoreController(db2, bdd.Configuration)).MaCommandeDetails(commandeAlice));
        Assert.IsType<ViewResult>(await boutiqueAlice.Pour(new StoreController(db2, bdd.Configuration)).AfficherFacture(factureAlice));
    }
}
