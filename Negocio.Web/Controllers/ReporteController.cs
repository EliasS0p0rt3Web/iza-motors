using Microsoft.AspNetCore.Mvc;
using Negocio.Web.Data;
using Negocio.Web.Models.ViewModels;

namespace Negocio.Web.Controllers
{
    public class ReporteController : Controller
    {
        private readonly NegocioDbContext _context;

        public ReporteController(NegocioDbContext context)
        {
            _context = context;
        }

        // =============================
        // RESUMEN GENERAL
        // =============================
        public IActionResult Resumen(DateTime? desde, DateTime? hasta)
        {
            // 🇵🇪 Zona horaria Perú
            var peruTimeZone = TimeZoneInfo.FindSystemTimeZoneById(
                "SA Pacific Standard Time"
            );

            var ahoraPeru = TimeZoneInfo.ConvertTimeFromUtc(
                DateTime.UtcNow,
                peruTimeZone
            );

            // =============================
            // 🗓️ DEFAULT: SEMANA ACTUAL
            // (LUNES A SÁBADO)
            // =============================
            int diff = (7 + (ahoraPeru.DayOfWeek - DayOfWeek.Monday)) % 7;
            var lunes = ahoraPeru.Date.AddDays(-diff);
            var sabado = lunes.AddDays(5);

            var desdeFinal = desde ?? lunes;
            var hastaFinal = hasta ?? sabado;

            // ⏰ RANGOS COMPLETOS
            var inicio = desdeFinal.Date;
            var fin = hastaFinal.Date.AddDays(1).AddTicks(-1);

            var vm = new ResumenGeneralViewModel
            {
                Desde = desdeFinal,
                Hasta = hastaFinal
            };

            // =============================
            // 🧾 VENTAS
            // =============================
            var ventas = _context.Ventas
                .Where(v =>
                    v.FechaRegistro >= inicio &&
                    v.FechaRegistro <= fin
                )
                .ToList();

            // ===== EFECTIVO =====
            vm.EfectivoAluminio = ventas
                .Where(v => v.Destino == "EFECTIVO" && v.Area == "ALUMINIO")
                .Sum(v => v.Precio);

            vm.EfectivoAccesorios = ventas
                .Where(v => v.Destino == "EFECTIVO" && v.Area == "ACCESORIOS")
                .Sum(v => v.Precio);

            vm.EfectivoARC = ventas
                .Where(v => v.Destino == "EFECTIVO" && v.Area == "ARC")
                .Sum(v => v.Precio);

            // ===== YAPE =====
            vm.YapeAluminio = ventas
                .Where(v => v.Destino == "YAPE" && v.Area == "ALUMINIO")
                .Sum(v => v.Precio);

            vm.YapeAccesorios = ventas
                .Where(v => v.Destino == "YAPE" && v.Area == "ACCESORIOS")
                .Sum(v => v.Precio);

            vm.YapeARC = ventas
                .Where(v => v.Destino == "YAPE" && v.Area == "ARC")
                .Sum(v => v.Precio);

            // ===== TOTALES =====
            vm.TotalEfectivo =
                vm.EfectivoAluminio +
                vm.EfectivoAccesorios +
                vm.EfectivoARC;

            vm.TotalYape =
                vm.YapeAluminio +
                vm.YapeAccesorios +
                vm.YapeARC;

            vm.TotalVentas = vm.TotalEfectivo + vm.TotalYape;

            // =============================
            // 💸 GASTOS
            // =============================
            vm.Gastos = _context.Gastos
                .Where(g =>
                    g.FechaRegistro >= inicio &&
                    g.FechaRegistro <= fin
                )
                .OrderByDescending(g => g.FechaRegistro)
                .ToList();

            vm.TotalGastos = vm.Gastos.Sum(g => g.Total);

            // =============================
            // 💵 CAJA (SOLO EFECTIVO)
            // =============================
            vm.CajaEfectivo = vm.TotalEfectivo - vm.TotalGastos;

            // =============================
            // 📊 RESULTADO FINAL
            // =============================
            vm.ResultadoFinal = vm.TotalVentas - vm.TotalGastos;

            return View(vm);
        }




        // =========================
        // 📅 VENTAS POR DÍA
        // =========================
        public IActionResult PorDia(DateTime? fecha)
        {
            // 🇵🇪 Zona horaria Perú
            var peruTimeZone = TimeZoneInfo.FindSystemTimeZoneById(
                "SA Pacific Standard Time"
            );

            var ahoraPeru = TimeZoneInfo.ConvertTimeFromUtc(
                DateTime.UtcNow,
                peruTimeZone
            );

            // Si no envían fecha → usar fecha actual Perú
            var dia = fecha ?? ahoraPeru.Date;

            // Rango completo del día (00:00:00 - 23:59:59)
            var inicio = dia.Date;
            var fin = dia.Date.AddDays(1).AddTicks(-1);

            var ventas = _context.Ventas
                .Where(v =>
                    v.FechaRegistro >= inicio &&
                    v.FechaRegistro <= fin
                )
                .OrderBy(v => v.FechaRegistro)
                .Select(v => new VentaProductoViewModel
                {
                    Fecha = v.FechaRegistro,
                    Area = v.Area,
                    Producto = v.Descripcion,
                    Dimensiones = v.Dimensiones,
                    Cantidad = v.Cantidad,
                    Unidad = v.Unidad,
                    Precio = v.Precio,
                    Destino = v.Destino
                })
                .ToList();

            ViewBag.Fecha = dia;
            ViewBag.TotalVentas = ventas.Sum(v => v.Precio);

            ViewBag.TotalEfectivo = ventas
                .Where(v => v.Destino == "EFECTIVO")
                .Sum(v => v.Precio);

            ViewBag.TotalYape = ventas
                .Where(v => v.Destino == "YAPE")
                .Sum(v => v.Precio);

            return View(ventas);
        }

        // =============================
        // 📦 VENTAS POR PRODUCTO
        // =============================
        public IActionResult VentasPorProducto(
    DateTime? desde,
    DateTime? hasta,
    string? area,
    string? producto,
    string? dimension)
        {
            var vm = new ReporteVentasProductoViewModel
            {
                Desde = desde,
                Hasta = hasta,
                NombreProducto = producto
            };

            ViewBag.Areas = _context.Ventas
                .Select(v => v.Area)
                .Distinct()
                .ToList();

            if (string.IsNullOrWhiteSpace(producto) ||
                string.IsNullOrWhiteSpace(area) ||
                desde == null || hasta == null)
                return View(vm);

            var inicio = desde.Value.Date;
            var fin = hasta.Value.Date.AddDays(1).AddTicks(-1);

            var ventas = _context.Ventas
                .Where(v =>
                    v.Area == area &&
                    v.Descripcion == producto &&
                    (string.IsNullOrEmpty(dimension) || v.Dimensiones == dimension) &&
                    v.FechaRegistro >= inicio &&
                    v.FechaRegistro <= fin)
                .ToList();

            vm.TotalCantidad = ventas.Sum(v => v.Cantidad);
            vm.TotalImporte = ventas.Sum(v => v.Precio);

            // 👇 Obtener unidad (asumimos que todas son iguales)
            vm.Unidad = ventas.Select(v => v.Unidad).FirstOrDefault();
            return View(vm);
        }


        // =============================
        // 📦 Obtener productos por área
        // =============================
        [HttpGet]
        public IActionResult ObtenerProductos(string area)
        {
            var productos = _context.Ventas
                .Where(v => v.Area == area)
                .Select(v => v.Descripcion)
                .Distinct()
                .OrderBy(p => p)
                .ToList();

            return Json(productos);
        }


        // =============================
        // 📐 Obtener dimensiones por producto
        // =============================
        [HttpGet]
        public IActionResult ObtenerDimensiones(string producto)
        {
            var dimensiones = _context.Ventas
                .Where(v => v.Descripcion == producto)
                .Select(v => v.Dimensiones)
                .Distinct()
                .OrderBy(d => d)
                .ToList();

            return Json(dimensiones);
        }

    }
}
