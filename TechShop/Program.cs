using Microsoft.EntityFrameworkCore;
using TechShop.Data;
using TechShop.Models.DAO;

namespace TechShop
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllersWithViews();

            var cnx = builder.Configuration.GetConnectionString("cnx");
            builder.Services.AddDbContext<AppDbContext>(options =>options.UseSqlServer(cnx));

            var app = builder.Build();

            // Crée / met à jour la base au démarrage (migrations EF + tables Categories et Marques)
            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                DbInitializer.Initialize(db);
            }

            app.UseHttpsRedirection();

            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Produit}/{action=Index}/{id?}");

            app.Run();
        }
    }
}