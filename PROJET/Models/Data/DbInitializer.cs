using Microsoft.EntityFrameworkCore;

namespace PROJET.Data
{
    /// <summary>
    /// Prépare la base au démarrage de l'application :
    /// 1. applique les migrations Entity Framework (Produits, Ventes, VenteLignes, Factures) ;
    /// 2. crée les tables Categories et Marques (gérées en ADO.NET par les DAO) si elles n'existent pas ;
    /// 3. ajoute quelques catégories et marques de démonstration si ces tables sont vides.
    /// Sans effet sur une base déjà initialisée.
    /// </summary>
    public static class DbInitializer
    {
        public static void Initialize(AppDbContext db)
        {
            db.Database.Migrate();

            db.Database.ExecuteSqlRaw(@"
IF OBJECT_ID(N'dbo.Categories', N'U') IS NULL
    CREATE TABLE dbo.Categories (
        Id       INT IDENTITY(1,1) PRIMARY KEY,
        Name     NVARCHAR(100) NOT NULL,
        IsActive BIT NOT NULL DEFAULT 1
    );

IF OBJECT_ID(N'dbo.Marques', N'U') IS NULL
    CREATE TABLE dbo.Marques (
        Id       INT IDENTITY(1,1) PRIMARY KEY,
        Name     NVARCHAR(100) NOT NULL,
        IsActive BIT NOT NULL DEFAULT 1
    );");

            db.Database.ExecuteSqlRaw(@"
IF NOT EXISTS (SELECT 1 FROM dbo.Categories)
    INSERT INTO dbo.Categories (Name) VALUES
        (N'Ordinateurs portables'), (N'Smartphones'), (N'Tablettes'), (N'Accessoires'), (N'Écrans');

IF NOT EXISTS (SELECT 1 FROM dbo.Marques)
    INSERT INTO dbo.Marques (Name) VALUES
        (N'Apple'), (N'Samsung'), (N'HP'), (N'Lenovo'), (N'Asus'), (N'Xiaomi');");
        }
    }
}
