namespace TechShop.Models
{
    /// <summary>Montants d'une facture, calculés selon les règles fiscales tunisiennes.</summary>
    public record MontantsFacture(decimal HT, decimal TVA, decimal Timbre, decimal TTC);

    /// <summary>
    /// Règles de facturation, isolées du contrôleur pour être testables :
    /// HT = somme des lignes (prix figé à la vente), TVA 19 % arrondie au millime,
    /// timbre fiscal de 1 DT, TTC = HT + TVA + timbre ; numéro F-AAAA-NNN.
    /// </summary>
    public static class CalculFacture
    {
        public static MontantsFacture Calculer(IEnumerable<VenteLigne> lignes)
        {
            decimal ht = lignes.Sum(l => l.PrixUnitaire * l.Quantite);
            decimal tva = Math.Round(ht * Facture.TauxTVA, 3);
            decimal timbre = Facture.MontantTimbre;
            return new MontantsFacture(ht, tva, timbre, ht + tva + timbre);
        }

        /// <param name="annee">Année de la facture.</param>
        /// <param name="rang">Rang de la facture dans l'année (1 pour la première).</param>
        public static string Numero(int annee, int rang) => $"F-{annee}-{rang:D3}";
    }
}
