using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using TechShop.Data;

namespace TechShop.Models
{
    /// <summary>
    /// Vérifie à chaque requête que le compte du cookie de connexion est toujours valable :
    /// un client bloqué (ou supprimé, ou dont le rôle a changé) est déconnecté immédiatement,
    /// sans attendre qu'il se déconnecte lui-même.
    /// </summary>
    public static class ControleCompte
    {
        public static bool EstToujoursAutorise(AppDbContext db, ClaimsPrincipal? principal)
        {
            var id = principal?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(id, out int userId)) return false;

            var user = db.Utilisateurs.AsNoTracking().FirstOrDefault(u => u.Id == userId);
            return user != null
                && !user.IsBlocked
                && user.Role == principal!.FindFirstValue(ClaimTypes.Role);
        }

        /// <summary>Branché sur l'évènement OnValidatePrincipal du cookie d'authentification.</summary>
        public static async Task ValiderAsync(CookieValidatePrincipalContext context)
        {
            var db = context.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
            if (!EstToujoursAutorise(db, context.Principal))
            {
                context.RejectPrincipal();
                await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            }
        }
    }
}
