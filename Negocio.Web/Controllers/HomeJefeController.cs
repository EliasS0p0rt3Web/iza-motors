using Microsoft.AspNetCore.Mvc;
using Negocio.Web.Data;

namespace Negocio.Web.Controllers
{
    public class HomeJefeController : Controller
    {
        private readonly NegocioDbContext _context;

        public HomeJefeController(NegocioDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            if (HttpContext.Session.GetString("ROL") != "JEFE")
                return RedirectToAction("Index", "Login");

            var hoy = DateTime.Today;

            ViewBag.VentasHoy = _context.Ventas
                .Where(v => v.FechaRegistro.Date == hoy)
                .Sum(v => v.Precio);

            ViewBag.CantidadVentas = _context.Ventas
                .Where(v => v.FechaRegistro.Date == hoy)
                .Count();

            var ultimaVenta = _context.Ventas
                .OrderByDescending(v => v.FechaRegistro)
                .Select(v => v.FechaRegistro)
                .FirstOrDefault();

            ViewBag.UltimaVenta = ultimaVenta;

            return View();
        }
    }
}
