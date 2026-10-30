using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using TechShop.Data;
using TechShop.Models;
using TechShop.Models.DAO;
using Microsoft.AspNetCore.Identity;

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

            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.LoginPath = "/Account/Login";
                    options.AccessDeniedPath = "/Account/Login";
                    // Un client bloqué par l'administrateur est déconnecté dès sa requête suivante
                    options.Events.OnValidatePrincipal = ControleCompte.ValiderAsync;
                });

            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(30);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });

            var app = builder.Build();

            using (var scope = app.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                // Crée / met à jour la base (migrations EF + tables ADO.NET Categories et Marques)
                DbInitializer.Initialize(context);

                if (!context.Utilisateurs.Any(u => u.Email == "admin@admin.com"))
                {
                    var passwordHasher = new PasswordHasher<Utilisateur>();
                    var admin = new Utilisateur
                    {
                        Nom = "Administrateur",
                        Email = "admin@admin.com",
                        Role = "Admin"
                    };
                    admin.MotDePasse = passwordHasher.HashPassword(admin, "admin123");
                    context.Utilisateurs.Add(admin);
                    context.SaveChanges();
                }
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseSession(); 
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Store}/{action=Index}/{id?}");

            app.Run();
        }
    }
}