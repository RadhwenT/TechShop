using System.Reflection;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TechShop.Controllers;
using TechShop.Models;

namespace TechShop.Tests;

/// <summary>
/// Comptes utilisateurs (inscription, connexion, mots de passe hachés, blocage par
/// l'administrateur) et contrôle d'accès du back-office.
/// </summary>
[Collection("Base")]
public class ComptesEtSecuriteTests(BaseDeDonnees bdd)
{
    private static string? Role(ClaimsPrincipal? p) => p?.FindFirst(ClaimTypes.Role)?.Value;

    // ---------- inscription ----------

    [Fact]
    public async Task InscriptionCreeUnClientAvecMotDePasseHacheEtLeConnecte()
    {
        var email = $"nouveau{Guid.NewGuid():N}@test.tn";
        var r = new Requete();
        using var db = bdd.NouveauContexte();

        var result = await r.Pour(new AccountController(db)).RegisterPost(
            new Utilisateur { Nom = "Nouveau client", Email = email, Role = "Admin" },   // tentative de s'attribuer le rôle Admin
            "MotDePasse!2026");

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Store", redirect.ControllerName);

        using var db2 = bdd.NouveauContexte();
        var u = db2.Utilisateurs.Single(x => x.Email == email);
        Assert.Equal("Client", u.Role);                           // le rôle est imposé côté serveur
        Assert.NotEqual("MotDePasse!2026", u.MotDePasse);         // jamais stocké en clair
        Assert.Equal(PasswordVerificationResult.Success,
            new PasswordHasher<Utilisateur>().VerifyHashedPassword(u, u.MotDePasse, "MotDePasse!2026"));
        Assert.Equal("Client", Role(r.Auth.Connecte));
    }

    [Fact]
    public async Task InscriptionAvecUnEmailDejaUtiliseRefusee()
    {
        var existant = Donnees.Utilisateur(bdd);
        using var db = bdd.NouveauContexte();
        var controller = new Requete().Pour(new AccountController(db));

        var result = await controller.RegisterPost(new Utilisateur { Nom = "Doublon", Email = existant.Email }, "autre123");

        Assert.IsType<ViewResult>(result);
        Assert.False(controller.ModelState.IsValid);
        Assert.Equal(1, bdd.NouveauContexte().Utilisateurs.Count(u => u.Email == existant.Email));
    }

    // ---------- connexion ----------

    [Fact]
    public async Task ConnexionAdministrateurVersLeTableauDeBord()
    {
        var admin = Donnees.Utilisateur(bdd, "Admin", "admin-pass");
        var r = new Requete();
        using var db = bdd.NouveauContexte();

        var redirect = Assert.IsType<RedirectToActionResult>(await r.Pour(new AccountController(db)).Login(admin.Email, "admin-pass"));

        Assert.Equal("Home", redirect.ControllerName);
        Assert.Equal("Admin", Role(r.Auth.Connecte));
    }

    [Fact]
    public async Task ConnexionClientVersLaBoutique()
    {
        var client = Donnees.Utilisateur(bdd, "Client", "client-pass");
        var r = new Requete();
        using var db = bdd.NouveauContexte();

        var redirect = Assert.IsType<RedirectToActionResult>(await r.Pour(new AccountController(db)).Login(client.Email, "client-pass"));

        Assert.Equal("Store", redirect.ControllerName);
        Assert.Equal(client.Id.ToString(), r.Auth.Connecte!.FindFirst(ClaimTypes.NameIdentifier)!.Value);
    }

    [Theory]
    [InlineData("mauvais-mot-de-passe")]
    [InlineData("")]
    public async Task MauvaisMotDePasseRefuse(string motDePasse)
    {
        var client = Donnees.Utilisateur(bdd, "Client", "bon-mot-de-passe");
        var r = new Requete();
        using var db = bdd.NouveauContexte();

        Assert.IsType<ViewResult>(await r.Pour(new AccountController(db)).Login(client.Email, motDePasse));
        Assert.Null(r.Auth.Connecte);
    }

    [Fact]
    public async Task EmailInconnuRefuse()
    {
        var r = new Requete();
        using var db = bdd.NouveauContexte();
        Assert.IsType<ViewResult>(await r.Pour(new AccountController(db)).Login("inconnu@test.tn", "x"));
        Assert.Null(r.Auth.Connecte);
    }

    // ---------- blocage d'un client ----------

    [Fact]
    public async Task UnClientBloqueParLAdministrateurNePeutPlusSeConnecter()
    {
        var client = Donnees.Utilisateur(bdd, "Client", "client-pass");
        var admin = new Requete().Connecte(Donnees.Utilisateur(bdd, "Admin"));

        using (var db = bdd.NouveauContexte())
            await admin.Pour(new ClientController(db)).ToggleBlock(client.Id);

        var r = new Requete();
        using (var db = bdd.NouveauContexte())
            Assert.IsType<ViewResult>(await r.Pour(new AccountController(db)).Login(client.Email, "client-pass"));
        Assert.Null(r.Auth.Connecte);

        using (var db = bdd.NouveauContexte())
            await admin.Pour(new ClientController(db)).ToggleBlock(client.Id);   // débloqué
        using (var db = bdd.NouveauContexte())
            Assert.IsType<RedirectToActionResult>(await r.Pour(new AccountController(db)).Login(client.Email, "client-pass"));
    }

    [Fact]
    public async Task UnAdministrateurNePeutPasEtreBloque()
    {
        var autreAdmin = Donnees.Utilisateur(bdd, "Admin");
        using var db = bdd.NouveauContexte();
        var result = await new Requete().Connecte(Donnees.Utilisateur(bdd, "Admin")).Pour(new ClientController(db)).ToggleBlock(autreAdmin.Id);

        Assert.IsType<NotFoundResult>(result);
        Assert.False(bdd.NouveauContexte().Utilisateurs.Single(u => u.Id == autreAdmin.Id).IsBlocked);
    }

    // ---------- contrôle d'accès ----------

    [Theory]
    [InlineData(typeof(HomeController))]
    [InlineData(typeof(ProduitController))]
    [InlineData(typeof(CategorieController))]
    [InlineData(typeof(MarqueController))]
    [InlineData(typeof(VenteController))]
    [InlineData(typeof(CommandeController))]
    [InlineData(typeof(ClientController))]
    [InlineData(typeof(LivreurController))]
    public void LeBackOfficeEstReserveALAdministrateur(Type controller)
    {
        var attribut = controller.GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(attribut);
        Assert.Equal("Admin", attribut!.Roles);
    }

    [Fact]
    public void LaCommandeEtLesPagesClientExigentUneConnexion()
    {
        Assert.NotNull(typeof(CartController).GetMethod(nameof(CartController.Checkout))!.GetCustomAttribute<AuthorizeAttribute>());
        foreach (var action in new[] { nameof(StoreController.MesCommandes), nameof(StoreController.MaCommandeDetails), nameof(StoreController.AfficherFacture) })
            Assert.NotNull(typeof(StoreController).GetMethod(action)!.GetCustomAttribute<AuthorizeAttribute>());
    }

    [Fact]
    public void ToutesLesActionsPostSontProtegeesContreLeCsrf()
    {
        var nonProtegees = typeof(AccountController).Assembly.GetTypes()
            .Where(t => typeof(Controller).IsAssignableFrom(t))
            .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            .Where(m => m.GetCustomAttribute<HttpPostAttribute>() != null
                     && m.GetCustomAttribute<ValidateAntiForgeryTokenAttribute>() == null)
            .Select(m => $"{m.DeclaringType!.Name}.{m.Name}")
            .ToList();
        Assert.Empty(nonProtegees);
    }

    [Fact]
    public void LeCatalogueEtLePanierRestentPublics()
    {
        Assert.Null(typeof(StoreController).GetCustomAttribute<AuthorizeAttribute>());
        Assert.Null(typeof(CartController).GetCustomAttribute<AuthorizeAttribute>());
        Assert.Null(typeof(StoreController).GetMethod(nameof(StoreController.Index))!.GetCustomAttribute<AuthorizeAttribute>());
    }
}
