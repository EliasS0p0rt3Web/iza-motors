using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Negocio.Web.Data;
using Negocio.Web.Models.Entities;
using System.Globalization;

namespace Negocio.Web.Controllers
{
    public class GastoController : Controller
    {
        private readonly NegocioDbContext _context;

        public GastoController(NegocioDbContext context)
        {
            _context = context;
        }

        private DateTime HoyPeru()
        {
            var peruTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SA Pacific Standard Time");
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, peruTimeZone).Date;
        }

        // ⚡ helper nuevo para no duplicar código de fechas
        private (DateTime Desde, DateTime Hasta) CalcularRangoFechas(DateTime? desde, DateTime? hasta)
        {
            if (desde.HasValue && hasta.HasValue)
            {
                return (desde.Value.Date, hasta.Value.Date);
            }

            var hoy = HoyPeru();
            int diff = (7 + (hoy.DayOfWeek - DayOfWeek.Monday)) % 7;
            DateTime lunes = hoy.AddDays(-diff);
            DateTime sabado = lunes.AddDays(5); // Mantiene tu rango Lunes a Sábado

            return (lunes, sabado);
        }

        // =============================
        // LISTADO (Optimizado + Async + NoTracking)
        // =============================
        public async Task<IActionResult> Index(DateTime? desde, DateTime? hasta, string? categoria, string? descripcion)
        {
            var (fechaDesde, fechaHasta) = CalcularRangoFechas(desde, hasta);

            // 🔥 CONSULTA SARGABLE: Evita usar .Date en el Where para que SQL use sus Índices.
            // Al sumar 1 día a 'hasta' y usar '<', agarramos todo el contenido de ese día eficientemente.
            var limiteHasta = fechaHasta.AddDays(1);

            // ⚡ .AsNoTracking() hace que EF no consuma memoria guardando copias de los objetos (Solo lectura)
            var query = _context.Gastos.AsNoTracking().AsQueryable();

            query = query.Where(g => g.FechaRegistro >= fechaDesde && g.FechaRegistro < limiteHasta);

            if (!string.IsNullOrWhiteSpace(categoria))
            {
                query = query.Where(g => g.Categoria == categoria);
            }

            if (!string.IsNullOrWhiteSpace(descripcion))
            {
                query = query.Where(g => g.Descripcion.Contains(descripcion));
            }

            // 🚀 Operación Asíncrona para no bloquear los hilos del servidor de Azure
            var gastos = await query
                .OrderByDescending(g => g.FechaRegistro)
                .Take(100) // ⚡ Paginación preventiva: Máximo 100 en la carga inicial estática
                .ToListAsync();

            ViewBag.Desde = fechaDesde;
            ViewBag.Hasta = fechaHasta;
            ViewBag.Categoria = categoria;
            ViewBag.Descripcion = descripcion;

            ViewBag.Categorias = new List<SelectListItem>
            {
                new SelectListItem { Value = "", Text = "Todas" },
                new SelectListItem { Value = "MENU", Text = "MENU" },
                new SelectListItem { Value = "PASAJE", Text = "PASAJE" },
                new SelectListItem { Value = "SEGURIDAD", Text = "SEGURIDAD" },
                new SelectListItem { Value = "LUZ", Text = "LUZ" },
                new SelectListItem { Value = "VUELTO DE YAPE", Text = "VUELTO DE YAPE" },
                new SelectListItem { Value = "OTROS", Text = "OTROS" }
            };

            return View(gastos);
        }

        // =============================
        // BUSQUEDA EN TIEMPO REAL (Fina y Ultra Rápida)
        // =============================
        [HttpGet]
        public async Task<IActionResult> BuscarGastos(DateTime? desde, DateTime? hasta, string? categoria, string? descripcion)
        {
            var (fechaDesde, fechaHasta) = CalcularRangoFechas(desde, hasta);
            var limiteHasta = fechaHasta.AddDays(1);

            var query = _context.Gastos.AsNoTracking().AsQueryable();

            query = query.Where(g => g.FechaRegistro >= fechaDesde && g.FechaRegistro < limiteHasta);

            if (!string.IsNullOrWhiteSpace(categoria))
            {
                query = query.Where(g => g.Categoria == categoria);
            }

            if (!string.IsNullOrWhiteSpace(descripcion))
            {
                query = query.Where(g => g.Descripcion.Contains(descripcion));
            }

            // Traemos solo los 50 necesarios de forma asíncrona
            var listaGastos = await query
                .OrderByDescending(g => g.FechaRegistro)
                .Take(50)
                .ToListAsync();

            // Formateamos la fecha en memoria local (evita errores de traducción a SQL)
            var resultado = listaGastos.Select(g => new
            {
                g.IdGasto,
                Fecha = g.FechaRegistro.ToString("dd/MM/yyyy"),
                g.Categoria,
                g.Descripcion,
                g.Total
            });

            return Json(resultado);
        }

        // =============================
        // FORM
        // =============================
        public IActionResult Registrar()
        {
            var hoy = HoyPeru();

            var gasto = new Gasto
            {
                FechaRegistro = hoy,
                Dia = hoy.ToString("dddd", new CultureInfo("es-PE")).ToUpper()
            };

            return View(gasto);
        }

        // =============================
        // GUARDAR (Async)
        // =============================
        [HttpPost]
        public async Task<IActionResult> Registrar(Gasto gasto)
        {
            if (!ModelState.IsValid)
                return View(gasto);

            await _context.Gastos.AddAsync(gasto);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // =============================
        // EDITAR (Async)
        // =============================
        public async Task<IActionResult> Editar(int id)
        {
            var gasto = await _context.Gastos
                .FirstOrDefaultAsync(g => g.IdGasto == id);

            if (gasto == null)
                return NotFound();

            return View(gasto);
        }

        [HttpPost]
        public async Task<IActionResult> Editar(Gasto gasto)
        {
            if (!ModelState.IsValid)
                return View(gasto);

            _context.Gastos.Update(gasto);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // =============================
        // ELIMINAR (Async)
        // =============================
        [HttpPost]
        public async Task<IActionResult> Eliminar(int id)
        {
            var gasto = await _context.Gastos
                .FirstOrDefaultAsync(g => g.IdGasto == id);

            if (gasto == null)
                return NotFound();

            _context.Gastos.Remove(gasto);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Gasto eliminado correctamente";

            return RedirectToAction(nameof(Index));
        }
    }
}