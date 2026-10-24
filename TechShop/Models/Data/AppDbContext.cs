using Microsoft.EntityFrameworkCore;
using TechShop.Models;

namespace TechShop.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<Produit> Produits { get; set; }

        // ── Module Vente ─────────────────────────────────────────────────────
        public DbSet<Vente> Ventes { get; set; }
        public DbSet<VenteLigne> VenteLignes { get; set; }
        public DbSet<Facture> Factures { get; set; }
        // Retour supprimé

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Une vente ne peut avoir qu'une seule facture (relation 1-1)
            modelBuilder.Entity<Facture>()
                .HasIndex(f => f.VenteID)
                .IsUnique();

            // Numéro de facture unique
            modelBuilder.Entity<Facture>()
                .HasIndex(f => f.NumeroFacture)
                .IsUnique();
        }
    }
}