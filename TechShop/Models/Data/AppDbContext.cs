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
        public DbSet<Utilisateur> Utilisateurs { get; set; }
        public DbSet<Livreur> Livreurs { get; set; }

        public DbSet<Vente> Ventes { get; set; }
        public DbSet<VenteLigne> VenteLignes { get; set; }
        public DbSet<Facture> Factures { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Utilisateur>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<Facture>()
                .HasIndex(f => f.VenteID)
                .IsUnique();

            modelBuilder.Entity<Facture>()
                .HasIndex(f => f.NumeroFacture)
                .IsUnique();
        }
    }
}
