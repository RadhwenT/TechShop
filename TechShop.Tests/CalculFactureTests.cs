using TechShop.Models;

namespace TechShop.Tests;

/// <summary>Règles de facturation (TVA 19 %, timbre 1 DT, numérotation), sans base de données.</summary>
public class CalculFactureTests
{
    private static VenteLigne Ligne(decimal prix, int quantite) => new() { PrixUnitaire = prix, Quantite = quantite };

    [Fact]
    public void CalculStandard()
    {
        var m = CalculFacture.Calculer([Ligne(1000m, 1)]);
        Assert.Equal(new MontantsFacture(1000m, 190m, 1m, 1191m), m);
    }

    [Fact]
    public void PlusieursLignesEtArrondiAuMillime()
    {
        // HT = 2 x 1250 + 3 x 19.99 = 2559.97 ; TVA = 486.3943 -> 486.394
        var m = CalculFacture.Calculer([Ligne(1250m, 2), Ligne(19.99m, 3)]);
        Assert.Equal(2559.97m, m.HT);
        Assert.Equal(486.394m, m.TVA);
        Assert.Equal(1m, m.Timbre);
        Assert.Equal(3047.364m, m.TTC);
    }

    [Fact]
    public void ArrondiBancaireSurLeDemiMillime()
    {
        // 0.05 x 0.19 = 0.0095 -> arrondi au pair le plus proche : 0.010 ; 0.15 x 0.19 = 0.0285 -> 0.028
        Assert.Equal(0.010m, CalculFacture.Calculer([Ligne(0.05m, 1)]).TVA);
        Assert.Equal(0.028m, CalculFacture.Calculer([Ligne(0.15m, 1)]).TVA);
    }

    [Fact]
    public void LeTimbreEstToujoursAjoute()
    {
        var m = CalculFacture.Calculer([]);
        Assert.Equal(0m, m.HT);
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
