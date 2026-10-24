using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using PROJET.Data;
using Testcontainers.MsSql;

namespace TechShop.Tests;

/// <summary>
/// Un vrai SQL Server jetable (Docker), initialisé exactement comme au démarrage de
/// l'application (migrations EF Core + tables ADO.NET + données de départ).
/// Partagé par toutes les classes de la collection "Base".
/// </summary>
public sealed class BaseDeDonnees : IAsyncLifetime
{
    private readonly MsSqlContainer _sql = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .Build();

    public string ConnectionString { get; private set; } = "";
    public IConfiguration Configuration { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _sql.StartAsync();
        ConnectionString = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(_sql.GetConnectionString())
        {
            InitialCatalog = "TechShopTests",
            TrustServerCertificate = true
        }.ConnectionString;
        Configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:cnx"] = ConnectionString })
            .Build();
        using var db = NouveauContexte();
        DbInitializer.Initialize(db);
    }

    public async Task DisposeAsync() => await _sql.DisposeAsync();

    public AppDbContext NouveauContexte() =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(ConnectionString).Options);

    /// <summary>Contrôleur prêt à l'emploi (TempData en mémoire), comme dans une requête.</summary>
    public static T AvecTempData<T>(T controller) where T : Controller
    {
        controller.TempData = new TempDataDictionary(new DefaultHttpContext(), new TempDataEnMemoire());
        return controller;
    }

    private sealed class TempDataEnMemoire : ITempDataProvider
    {
        private IDictionary<string, object> _data = new Dictionary<string, object>();
        public IDictionary<string, object> LoadTempData(HttpContext context) => _data;
        public void SaveTempData(HttpContext context, IDictionary<string, object> values) => _data = values;
    }
}

[CollectionDefinition("Base")]
public class CollectionBase : ICollectionFixture<BaseDeDonnees> { }
