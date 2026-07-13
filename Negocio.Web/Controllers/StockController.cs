using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Negocio.Web.Data;
using Negocio.Web.Models.ViewModels;

namespace Negocio.Web.Controllers
{
    public class StockController : Controller
    {
        private readonly NegocioDbContext _context;

        public StockController(NegocioDbContext context)
        {
            _context = context;
        }

        // =========================
        // 📦 CONSULTAR STOCK
        // =========================
        public IActionResult Index(string? area, bool critico = false)
        {
            var query = _context.Productos.AsQueryable();

            // FILTRO POR ÁREA
            if (!string.IsNullOrEmpty(area))
                query = query.Where(p => p.Area == area);

            // FILTRO STOCK CRÍTICO
            if (critico)
                query = query.Where(p => p.StockActual <= 5); // 🔥 umbral crítico

            var stock = query
    .OrderBy(p => p.Area)
    .ThenBy(p => p.Descripcion)
    .Select(p => new StockViewModel
    {
        IdProducto = p.IdProducto, // 👈 ESTA LÍNEA ES LA CLAVE
        Area = p.Area,
        Producto = p.Descripcion,
        Dimensiones = p.Dimensiones,
        Unidad = p.Unidad,
        StockActual = p.StockActual
    })
    .ToList();


            ViewBag.Areas = _context.Productos
                .Select(p => p.Area)
                .Distinct()
                .OrderBy(a => a)
                .ToList();

            ViewBag.AreaSeleccionada = area;
            ViewBag.EsCritico = critico;

            return View(stock);
        }

        [HttpGet]
        public IActionResult Buscar(string? area, string? buscar)
        {
            var query = _context.Productos.AsQueryable();

            if (!string.IsNullOrEmpty(area))
                query = query.Where(p => p.Area == area);

            if (!string.IsNullOrEmpty(buscar))
                query = query.Where(p => p.Descripcion.ToLower()
                                .Contains(buscar.ToLower()));

            var resultados = query
                .OrderBy(p => p.Descripcion)
                .Select(p => new
                {
                    p.IdProducto,
                    p.Area,
                    Producto = p.Descripcion,
                    p.Dimensiones,
                    p.Unidad,
                    p.StockActual
                })
                .Take(50) // límite seguridad
                .ToList();

            return Json(resultados);
        }

        // =========================
        // 🔥 MÁS VENDIDO (YA EXISTE)
        // =========================
        public IActionResult MasVendido(DateTime? desde, DateTime? hasta)
        {
            var fechaDesde = desde ?? DateTime.Today.AddDays(-7);
            var fechaHasta = hasta ?? DateTime.Today;

            var masVendidos = _context.Ventas
                .Where(v => v.FechaRegistro.Date >= fechaDesde.Date &&
                            v.FechaRegistro.Date <= fechaHasta.Date)
                .GroupBy(v => new { v.Area, v.Descripcion, v.Unidad })
                .Select(g => new MasVendidoViewModel
                {
                    Area = g.Key.Area,
                    Producto = g.Key.Descripcion,
                    Unidad = g.Key.Unidad,
                    CantidadTotal = g.Sum(x => x.Cantidad)
                })
                .OrderByDescending(x => x.CantidadTotal)
                .ToList();

            ViewBag.Desde = fechaDesde;
            ViewBag.Hasta = fechaHasta;

            return View(masVendidos);
        }

        [HttpPost]
        public IActionResult ActualizarStock(int IdProducto, decimal NuevoStock)
        {
            var rol = HttpContext.Session.GetString("ROL");

            if (rol != "ADMINISTRADOR")
            {
                TempData["Error"] = "No tienes permisos para modificar el stock";
                return RedirectToAction("Index");
            }

            var producto = _context.Productos.FirstOrDefault(p => p.IdProducto == IdProducto);

            if (producto == null)
                return NotFound();

            if (NuevoStock < 0)
            {
                TempData["Error"] = "El stock no puede ser negativo";
                return RedirectToAction("Index");
            }

            producto.StockActual = NuevoStock;
            _context.SaveChanges();

            TempData["Success"] = "Stock actualizado correctamente";
            return RedirectToAction("Index");
        }
    }
}
