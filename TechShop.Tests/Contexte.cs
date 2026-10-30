using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;
using TechShop.Data;
using TechShop.Models;
using TechShop.Models.DAO;

namespace TechShop.Tests;

/// <summary>
/// Simule une requête HTTP pour appeler un contrôleur comme le ferait ASP.NET Core :
/// utilisateur connecté (claims), session (panier), TempData et connexion par cookie.
/// </summary>
public sealed class Requete
{
    public FauxAuthentification Auth { get; } = new();
    public SessionEnMemoire Session { get; } = new();
    public ClaimsPrincipal Utilisateur { get; set; } = new(new ClaimsIdentity());

    /// <summary>Connecte un utilisateur (comme après un login réussi).</summary>
    public Requete Connecte(Utilisateur u)
    {
        Utilisateur = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, u.Id.ToString()),
            new Claim(ClaimTypes.Name, u.Nom),
            new Claim(ClaimTypes.Email, u.Email),
            new Claim(ClaimTypes.Role, u.Role)
        ], "Cookies"));
        return this;
    }

    public T Pour<T>(T controller) where T : Controller
    {
        var services = new ServiceCollection()
            .AddSingleton<IAuthenticationService>(Auth)
            .BuildServiceProvider();
        var http = new DefaultHttpContext { RequestServices = services, User = Utilisateur };
        http.Features.Set<ISessionFeature>(new SessionFeature { Session = Session });
        controller.ControllerContext = new ControllerContext { HttpContext = http };
        controller.TempData = new TempDataDictionary(http, new TempDataEnMemoire());
        return controller;
    }

    private sealed class TempDataEnMemoire : ITempDataProvider
    {
        private IDictionary<string, object> _data = new Dictionary<string, object>();
        public IDictionary<string, object> LoadTempData(HttpContext context) => _data;
        public void SaveTempData(HttpContext context, IDictionary<string, object> values) => _data = values;
    }
}

/// <summary>Enregistre les connexions / déconnexions au lieu d'écrire un cookie.</summary>
public sealed class FauxAuthentification : IAuthenticationService
{
    public ClaimsPrincipal? Connecte { get; private set; }

    public Task SignInAsync(HttpContext context, string? scheme, ClaimsPrincipal principal, AuthenticationProperties? properties)
    {
        Connecte = principal;
        return Task.CompletedTask;
    }

    public Task SignOutAsync(HttpContext context, string? scheme, AuthenticationProperties? properties)
    {
        Connecte = null;
        return Task.CompletedTask;
    }

    public Task<AuthenticateResult> AuthenticateAsync(HttpContext context, string? scheme) =>
        Task.FromResult(AuthenticateResult.NoResult());
    public Task ChallengeAsync(HttpContext context, string? scheme, AuthenticationProperties? properties) => Task.CompletedTask;
    public Task ForbidAsync(HttpContext context, string? scheme, AuthenticationProperties? properties) => Task.CompletedTask;
}

public sealed class SessionEnMemoire : ISession
{
    private readonly Dictionary<string, byte[]> _valeurs = new();
    public bool IsAvailable => true;
    public string Id { get; } = Guid.NewGuid().ToString();
    public IEnumerable<string> Keys => _valeurs.Keys;
    public void Clear() => _valeurs.Clear();
    public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public void Remove(string key) => _valeurs.Remove(key);
    public void Set(string key, byte[] value) => _valeurs[key] = value;
    public bool TryGetValue(string key, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out byte[]? value) =>
        _valeurs.TryGetValue(key, out value);
}

/// <summary>Création rapide de données de test (valeurs uniques pour ne pas dépendre des autres tests).</summary>
public static class Donnees
{
    public static int Produit(BaseDeDonnees bdd, decimal prix, int stock)
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

    public static int Stock(BaseDeDonnees bdd, int produitId)
    {
        using var db = bdd.NouveauContexte();
        return db.Produits.Single(p => p.ProduitID == produitId).Quantite;
    }

    public static Utilisateur Utilisateur(BaseDeDonnees bdd, string role = "Client", string motDePasse = "secret123", bool bloque = false)
    {
        using var db = bdd.NouveauContexte();
        var u = new Utilisateur
        {
            Nom = "Utilisateur test",
            Email = $"u{Guid.NewGuid():N}@test.tn",
            Role = role,
            IsBlocked = bloque,
            Telephone = "20000000",
            Adresse = "Sfax"
        };
        u.MotDePasse = new PasswordHasher<Utilisateur>().HashPassword(u, motDePasse);
        db.Utilisateurs.Add(u);
        db.SaveChanges();
        return u;
    }

    public static int Livreur(BaseDeDonnees bdd)
    {
        using var db = bdd.NouveauContexte();
        var l = new Livreur { Nom = "Livreur", Prenom = "Test", Telephone = "21000000" };
        db.Livreurs.Add(l);
        db.SaveChanges();
        return l.Id;
    }
}
