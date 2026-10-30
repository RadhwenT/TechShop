using TechShop.Models;

namespace TechShop.Tests;

/// <summary>
/// Règles de facturation, sans base de données : prix du catalogue TTC (TVA 19 % incluse),
/// HT = TTC / 1,19 arrondi au millime, TVA = TTC - HT, timbre fiscal 1 DT, numérotation.
/// </summary>
public class CalculFactureTests
{
    private static VenteLigne Ligne(decimal prix, int quantite) => new() { PrixUnitaire = prix, Quantite = quantite };

    [Fact]
    public void CalculStandard()
    {
        // 1 190 DT TTC = 1 000 DT HT + 190 DT de TVA ; net à payer = 1 190 + 1 DT de timbre
        var m = CalculFacture.Calculer([Ligne(1190m, 1)]);
        Assert.Equal(new MontantsFacture(1000m, 190m, 1m, 1191m), m);
    }

    [Fact]
    public void PlusieursLignesEtArrondiAuMillime()
    {
        // TTC = 2 x 1250 + 3 x 19.99 = 2559.97 ; HT = 2559.97 / 1.19 = 2151.2352... -> 2151.235
        var m = CalculFacture.Calculer([Ligne(1250m, 2), Ligne(19.99m, 3)]);
        Assert.Equal(2151.235m, m.HT);
        Assert.Equal(408.735m, m.TVA);
        Assert.Equal(1m, m.Timbre);
        Assert.Equal(2560.97m, m.TTC);
    }

    [Theory]
    [InlineData(10)]
    [InlineData(100)]
    [InlineData(19.99)]
    [InlineData(2559.97)]
    [InlineData(123456.78)]
    public void HtPlusTvaRedonneToujoursLePrixTtc(double prix)
    {
        decimal ttc = (decimal)prix;
        var m = CalculFacture.Calculer([Ligne(ttc, 1)]);
        Assert.Equal(ttc, m.HT + m.TVA);          // aucun millime perdu par l'arrondi
        Assert.Equal(ttc + 1m, m.TTC);
        Assert.Equal(m.HT, Math.Round(m.HT, 3));  // HT au millime
    }

    [Fact]
    public void LeTimbreEstToujoursAjoute()
    {
        var m = CalculFacture.Calculer([]);
        Assert.Equal(0m, m.HT);
        Assert.Equal(0m, m.TVA);
        Assert.Equal(1m, m.TTC);
    }

    [Theory]
    [InlineData(2026, 1, "F-2026-001")]
    [InlineData(2026, 42, "F-2026-042")]
    [InlineData(2027, 1234, "F-2027-1234")]
    public void FormatDuNumeroDeFacture(int annee, int rang, string attendu)
    {
        Assert.Equal(attendu, CalculFacture.Numero(annee, rang));
    }
}
