namespace TechShop.Models
{
    /// <summary>Montants d'une facture, calculés selon les règles fiscales tunisiennes.</summary>
    public record MontantsFacture(decimal HT, decimal TVA, decimal Timbre, decimal TTC);

    /// <summary>
    /// Règles de facturation, isolées du contrôleur pour être testables.
    /// Les prix du catalogue sont TTC (TVA 19 % incluse) :
    /// total des lignes = TTC des produits, HT = total / 1,19 arrondi au millime,
    /// TVA = total - HT, timbre fiscal de 1 DT, net à payer = total + timbre ; numéro F-AAAA-NNN.
    /// </summary>
    public static class CalculFacture
    {
        public static MontantsFacture Calculer(IEnumerable<VenteLigne> lignes)
        {
            decimal totalProduitsTTC = lignes.Sum(l => l.PrixUnitaire * l.Quantite);
            decimal ht = Math.Round(totalProduitsTTC / (1 + Facture.TauxTVA), 3);
            decimal tva = totalProduitsTTC - ht;
            decimal timbre = Facture.MontantTimbre;
            return new MontantsFacture(ht, tva, timbre, totalProduitsTTC + timbre);
        }

        /// <param name="annee">Année de la facture.</param>
        /// <param name="rang">Rang de la facture dans l'année (1 pour la première).</param>
        public static string Numero(int annee, int rang) => $"F-{annee}-{rang:D3}";
    }
}
