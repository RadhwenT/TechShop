using TechShop.Models;
using TechShop.Models.DAO;

namespace TechShop.Tests;

/// <summary>Accès aux données en ADO.NET (pattern DAO) sur un vrai SQL Server.</summary>
[Collection("Base")]
public class MarquesCategoriesDaoTests(BaseDeDonnees bdd)
{
    private static string Unique(string prefixe) => prefixe + Guid.NewGuid().ToString("N")[..8];

    [Fact]
    public void DonneesDeDepartCreeesAuDemarrage()
    {
        var marques = new MarqueDAO(bdd.Configuration).GetAll().Select(m => m.Name).ToList();
        var categories = new CategorieDAO(bdd.Configuration).GetAll().Select(c => c.Name).ToList();
        Assert.Contains("Apple", marques);
        Assert.Contains("Lenovo", marques);
        Assert.Contains("Smartphones", categories);
        Assert.Contains("Écrans", categories);
    }

    [Fact]
    public void AjoutRechercheParDebutDeNomEtDoublon()
    {
        var dao = new MarqueDAO(bdd.Configuration);
        var nom = Unique("Marque");
        Assert.True(dao.AddMarque(new Marque { Name = nom }));

        Assert.Single(dao.GetByName(nom[..10]), m => m.Name == nom);
        Assert.True(dao.ExistsName(nom.ToUpperInvariant()));   // insensible à la casse
        Assert.False(dao.ExistsName(Unique("Autre")));
    }

    [Fact]
    public void SuppressionLogiqueEtRestaurationDUneMarque()
    {
        var dao = new MarqueDAO(bdd.Configuration);
        var nom = Unique("Marque");
        dao.AddMarque(new Marque { Name = nom });
        var id = dao.GetByName(nom).Single().Id;

        Assert.True(dao.DeleteMarque(id));
        Assert.DoesNotContain(dao.GetAll(), m => m.Id == id);
        Assert.Contains(dao.GetDeleted(), m => m.Id == id);

        Assert.True(dao.RestoreMarque(id));
        Assert.Contains(dao.GetAll(), m => m.Id == id);
    }

    [Fact]
    public void ModificationDUneCategorie()
    {
        var dao = new CategorieDAO(bdd.Configuration);
        var nom = Unique("Categorie");
        dao.AddCategorie(new Categorie { Name = nom });
        var cat = dao.GetByName(nom).Single();

        var nouveau = Unique("Renommee");
        Assert.True(dao.UpdateCategorie(new Categorie { Id = cat.Id, Name = nouveau }));
        Assert.Contains(dao.GetAll(), c => c.Id == cat.Id && c.Name == nouveau);
    }

    [Fact]
    public void LesApostrophesNeCassentPasLesRequetes()
    {
        // Requêtes paramétrées : un nom contenant ' est enregistré tel quel.
        var dao = new MarqueDAO(bdd.Configuration);
        var nom = "L'" + Unique("Atelier");
        Assert.True(dao.AddMarque(new Marque { Name = nom }));
        Assert.True(dao.ExistsName(nom));
    }
}
