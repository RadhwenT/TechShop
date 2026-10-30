using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TechShop.Data;
using TechShop.Models;
using System.Security.Claims;

namespace TechShop.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _context;

        public AccountController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string email, string password)
        {
            var user = _context.Utilisateurs.FirstOrDefault(u => u.Email == email);
            if (user != null)
            {
                if (user.IsBlocked)
                {
                    ViewBag.Error = "Votre compte a été suspendu. Veuillez contacter l'administrateur.";
                    return View();
                }

                var hasher = new PasswordHasher<Utilisateur>();
                var result = hasher.VerifyHashedPassword(user, user.MotDePasse, password);

                if (result == PasswordVerificationResult.Success)
                {
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                        new Claim(ClaimTypes.Name, user.Nom),
                        new Claim(ClaimTypes.Email, user.Email),
                        new Claim(ClaimTypes.Role, user.Role)
                    };

                    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var principal = new ClaimsPrincipal(identity);

                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

                    if (user.Role == "Admin")
                        return RedirectToAction("Index", "Home");
                    else
                        return RedirectToAction("Index", "Store");
                }
            }

            ViewBag.Error = "Email ou mot de passe incorrect.";
            return View();
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegisterPost(Utilisateur model, string clearPassword)
        {
            ModelState.Remove("Role");
            ModelState.Remove("MotDePasse"); 

            if (string.IsNullOrWhiteSpace(clearPassword))
            {
                ModelState.AddModelError("", "Le mot de passe est obligatoire.");
            }

            if (ModelState.IsValid)
            {
                if (_context.Utilisateurs.Any(u => u.Email == model.Email))
                {
                    ModelState.AddModelError("Email", "Cet email est déjà utilisé.");
                    return View("Register", model);
                }

                model.Role = "Client";
                var hasher = new PasswordHasher<Utilisateur>();
                model.MotDePasse = hasher.HashPassword(model, clearPassword);

                _context.Utilisateurs.Add(model);
                await _context.SaveChangesAsync();

                // Login
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, model.Id.ToString()),
                    new Claim(ClaimTypes.Name, model.Nom),
                    new Claim(ClaimTypes.Email, model.Email),
                    new Claim(ClaimTypes.Role, model.Role)
                };
                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

                return RedirectToAction("Index", "Store");
            }
            return View("Register", model);
        }

        [HttpGet]
        public async Task<IActionResult> Manage()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int userId))
            {
                return RedirectToAction("Login");
            }

            var user = await _context.Utilisateurs.FindAsync(userId);
            if (user == null) return NotFound();

            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Manage(Utilisateur model, string? newPassword)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int userId))
            {
                return RedirectToAction("Login");
            }

            var user = await _context.Utilisateurs.FindAsync(userId);
            if (user == null) return NotFound();

            ModelState.Remove("MotDePasse");
            ModelState.Remove("Role");

            if (ModelState.IsValid)
            {
                if (_context.Utilisateurs.Any(u => u.Email == model.Email && u.Id != userId))
                {
                    ModelState.AddModelError("Email", "Cet email est déjà utilisé.");
                    return View(user);
                }

                user.Nom = model.Nom;
                user.Email = model.Email;
                user.Telephone = model.Telephone;
                user.Adresse = model.Adresse;

                if (!string.IsNullOrWhiteSpace(newPassword))
                {
                    var hasher = new PasswordHasher<Utilisateur>();
                    user.MotDePasse = hasher.HashPassword(user, newPassword);
                }

                await _context.SaveChangesAsync();

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Name, user.Nom),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Role, user.Role)
                };
                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

                TempData["Success"] = "Compte mis à jour.";
                return RedirectToAction("Manage");
            }

            return View(model);
        }


        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Store");
        }
    }
}
