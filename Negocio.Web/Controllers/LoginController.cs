using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using Negocio.Web.Data;
using Negocio.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Negocio.Web.Controllers
{
    public class LoginController : Controller
    {
        private readonly NegocioDbContext _context;

        public LoginController(NegocioDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Index() => View();

        [HttpPost]
        public async Task<IActionResult> Index(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                ViewBag.Error = "Todos los campos son obligatorios";
                return View();
            }

            var user = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.Username == username.Trim() && u.Activo);

            if (user != null && user.PasswordHash == "OAUTH_GOOGLE_EXTERNAL_ACCOUNT")
            {
                ViewBag.Error = "Esta cuenta está registrada de forma segura con Google. Por favor, inicia sesión presionando el botón 'Continuar con Google'.";
                return View();
            }

            try
            {
                if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
                {
                    ViewBag.Error = "Usuario o contraseña incorrectos";
                    return View();
                }
            }
            catch (Exception)
            {
                ViewBag.Error = "Usuario o contraseña incorrectos";
                return View();
            }

            HttpContext.Session.SetString("ROL", user.Rol);
            HttpContext.Session.SetString("USER", user.Username);
            HttpContext.Session.SetInt32("USER_ID", user.IdUsuario);

            var claims = new List<Claim>
    {
        new Claim(ClaimTypes.Name, user.Username),
        new Claim(ClaimTypes.Role, user.Rol),
        new Claim(ClaimTypes.NameIdentifier, user.IdUsuario.ToString())
    };

            var identity = new ClaimsIdentity(claims, "Cookies");
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync("Cookies", principal);

            if (user.Rol == "ADMINISTRADOR")
            {
                return RedirectToAction("Admin", "Home");
            }
            if (user.Rol == "JEFE")
            {
                return RedirectToAction("Jefe", "Home");
            }
            if (user.Rol == "CLIENTE")
            {
                TempData["Bienvenida"] = "¡Hola, " + user.Username + "! Bienvenido de nuevo.";
                return RedirectToAction("WebPrincipal", "Cliente");
            }

            return RedirectToAction("WebPrincipal", "Cliente");
        }

        [HttpGet]
        public IActionResult Registrar() => View();

        [HttpPost]
        public async Task<IActionResult> Registrar(string nombreCompleto, string email, string telefono, string password, string confirmPassword)
        {
            if (string.IsNullOrWhiteSpace(nombreCompleto) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(telefono) || string.IsNullOrWhiteSpace(password))
            {
                ViewBag.Error = "Todos los campos son obligatorios";
                return View();
            }

            if (password != confirmPassword)
            {
                ViewBag.Error = "Las contraseñas no coinciden";
                return View();
            }

            var existe = await _context.Usuarios
                .AnyAsync(u => u.Username.ToLower() == email.Trim().ToLower());

            if (existe)
            {
                ViewBag.Error = "Este correo electrónico ya está registrado";
                return View();
            }

            string passwordHashSeguro = BCrypt.Net.BCrypt.HashPassword(password);
            var nuevoUsuario = new Usuario
            {
                Username = email.Trim().ToLower(),
                PasswordHash = passwordHashSeguro,
                Rol = "CLIENTE",
                Activo = true
            };

            _context.Usuarios.Add(nuevoUsuario);
            await _context.SaveChangesAsync(); 
            return await Index(email.Trim().ToLower(), password);
        }

        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            HttpContext.Session.Clear();
            await HttpContext.SignOutAsync("Cookies");
            TempData["Despedida"] = "¡Sesión cerrada! Esperamos verte pronto en E&E Aluminios.";
            return RedirectToAction("Index", "HomePublic");
        }


        [HttpGet]
        public IActionResult LoginGoogle()
        {
            var properties = new AuthenticationProperties { RedirectUri = Url.Action("GoogleCallback") };
            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }

        [HttpGet]
        public async Task<IActionResult> GoogleCallback()
        {
            var result = await HttpContext.AuthenticateAsync("Cookies");

            if (!result.Succeeded)
            {
                ViewBag.Error = "Sincronización cancelada o fallida con Google.";
                return View("Index");
            }

            var email = result.Principal.FindFirstValue(ClaimTypes.Email)?.Trim().ToLower();
            var name = result.Principal.FindFirstValue(ClaimTypes.Name)?.Trim();

            if (string.IsNullOrEmpty(email))
            {
                ViewBag.Error = "Google no proporcionó una dirección de correo válida.";
                return View("Index");
            }
            var user = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.Username == email && u.Activo);

            if (user == null)
            {
                user = new Usuario
                {
                    Username = email,
                    PasswordHash = "OAUTH_GOOGLE_EXTERNAL_ACCOUNT", 
                    Rol = "CLIENTE",
                    Activo = true
                };

                _context.Usuarios.Add(user);
                await _context.SaveChangesAsync(); 

            }

            HttpContext.Session.SetString("ROL", user.Rol);
            HttpContext.Session.SetString("USER", user.Username);
            HttpContext.Session.SetInt32("USER_ID", user.IdUsuario);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.Rol),
                new Claim(ClaimTypes.NameIdentifier, user.IdUsuario.ToString())
            };

            var identity = new ClaimsIdentity(claims, "Cookies");
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync("Cookies", principal);

            TempData["Bienvenida"] = $"¡Hola, {user.Username}! Autenticado correctamente con Google.";

            return RedirectToAction("WebPrincipal", "Cliente");
        }
    }
}