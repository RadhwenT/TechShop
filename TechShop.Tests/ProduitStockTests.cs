using TechShop.Models;

namespace TechShop.Tests;

/// <summary>Règles de stock portées par l'entité Produit (aucune base nécessaire).</summary>
public class ProduitStockTests
{
    [Fact]
    public void UnProduitNeufNAPasDeStock()
    {
        Assert.Equal(0, new Produit().Quantite);
    }

    [Fact]
    public void AjouterPuisRetirerDuStock()
    {
        var p = new Produit();
        p.AjouterStock(10);
        p.RetirerStock(3);
        Assert.Equal(7, p.Quantite);
    }

    [Fact]
    public void OnPeutViderLeStockExactement()
    {
        var p = new Produit();
        p.AjouterStock(4);
        p.RetirerStock(4);
        Assert.Equal(0, p.Quantite);
    }

    [Fact]
    public void RetirerPlusQueLeStockEstRefuse()
    {
        var p = new Produit();
        p.AjouterStock(2);
        var ex = Assert.Throws<InvalidOperationException>(() => p.RetirerStock(3));
        Assert.Equal("Stock insuffisant", ex.Message);
        Assert.Equal(2, p.Quantite);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void LesQuantitesNulles0uNegativesSontRefusees(int quantite)
    {
        var p = new Produit();
        p.AjouterStock(5);
        Assert.Throws<ArgumentException>(() => p.AjouterStock(quantite));
        Assert.Throws<ArgumentException>(() => p.RetirerStock(quantite));
        Assert.Equal(5, p.Quantite);
    }

    [Fact]
    public void ConstantesFiscalesTunisiennes()
    {
        Assert.Equal(0.19m, Facture.TauxTVA);
        Assert.Equal(1m, Facture.MontantTimbre);
    }
}
