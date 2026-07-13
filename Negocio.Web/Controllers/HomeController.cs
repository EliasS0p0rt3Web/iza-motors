using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Negocio.Web.Data;
using Negocio.Web.Models;
using Negocio.Web.Models.Entities;

namespace Negocio.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly NegocioDbContext _context;

        public HomeController(
            ILogger<HomeController> logger,
            NegocioDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        // --- 🧠 MÉTODO COMPLEMENTARIO INTERNO (LUNES DE LA SEMANA) ---
        private DateTime ObtenerLunesDeSemana(DateTime fecha)
        {
            int dif = (7 + (fecha.DayOfWeek - DayOfWeek.Monday)) % 7;
            return fecha.AddDays(-1 * dif).Date;
        }

        // 🟢 HOME ADMIN (DASHBOARD)
        public async Task<IActionResult> Admin()
        {
            if (HttpContext.Session.GetString("ROL") != "ADMINISTRADOR")
                return RedirectToAction("Index", "Login");

            // 🇵🇪 Zona horaria Perú
            var peruTimeZone = TimeZoneInfo.FindSystemTimeZoneById(
                "SA Pacific Standard Time"
            );

            // ⏰ Hora actual en Perú
            var ahoraPeru = TimeZoneInfo.ConvertTimeFromUtc(
                DateTime.UtcNow,
                peruTimeZone
            );

            var inicioDia = ahoraPeru.Date;
            var finDia = inicioDia.AddDays(1).AddTicks(-1);

            // 📊 Ventas del día (RANGO, no Date ==)
            var ventasHoy = _context.Ventas
                .Where(v =>
                    v.FechaRegistro >= inicioDia &&
                    v.FechaRegistro <= finDia
                )
                .ToList();

            // 💸 Gastos del día
            var gastosHoy = _context.Gastos
                .Where(g =>
                    g.FechaRegistro >= inicioDia &&
                    g.FechaRegistro <= finDia
                )
                .Sum(g => g.Total);

            ViewBag.VentasTotal = ventasHoy.Sum(v => v.Precio);

            ViewBag.VentasEfectivo = ventasHoy
                .Where(v => v.Destino == "EFECTIVO")
                .Sum(v => v.Precio);

            ViewBag.VentasYape = ventasHoy
                .Where(v => v.Destino == "YAPE")
                .Sum(v => v.Precio);

            ViewBag.GastosHoy = gastosHoy;
            ViewBag.ResultadoHoy = ViewBag.VentasTotal - gastosHoy;

            // =========================
            // 🔹 RESUMEN RESERVAS
            // =========================

            ViewBag.ReservasPendientes = _context.Reservas
                .Count(r => r.Estado == "PENDIENTE");

            ViewBag.ReservasProduccion = _context.Reservas
                .Count(r => r.Estado == "EN_PRODUCCION");

            ViewBag.ReservasListas = _context.Reservas
                .Count(r => r.Estado == "LISTO");

            // Reservas para hoy
            ViewBag.ReservasHoy = _context.Reservas
                .Count(r => r.FechaSolicitada != null &&
                            r.FechaSolicitada.Value.Date == inicioDia);

            // =========================================================================
            // 🔥 INTEGRACIÓN: CARGAR INYECCIÓN DE MÉTRICAS GLOBALES Y ALERTAS
            // =========================================================================
            await CargarMetricasYAlertasSemanales(ahoraPeru);

            return View();
        }

        // 🚀 ACCIÓN PARA CAMBIAR EL ESTADO DE LA TIENDA DESDE EL SWITCH DEL ADMIN
        [HttpPost]
        public async Task<IActionResult> ActualizarEstadoTienda(bool estaAbierto)
        {
            try
            {
                // Buscamos el registro único de configuración en la BD (Id = 1) -> Corregido con ==
                var config = _context.ConfiguracionesTienda.FirstOrDefault(x => x.Id == 1);

                if (config == null)
                {
                    // Por seguridad, si no existiera lo creamos
                    config = new Negocio.Web.Models.Entities.ConfiguracionTienda
                    {
                        Id = 1,
                        EstaAbierto = estaAbierto,
                        MensajeEstado = estaAbierto ? "Abierto" : "Cerrado",
                        UltimaActualizacion = DateTime.Now
                    };
                    _context.ConfiguracionesTienda.Add(config);
                }
                else
                {
                    // Si ya existe, actualizamos los valores
                    config.EstaAbierto = estaAbierto;
                    config.MensajeEstado = estaAbierto ? "Abierto" : "Cerrado";
                    config.UltimaActualizacion = DateTime.Now;
                    _context.ConfiguracionesTienda.Update(config);
                }

                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }


        // 🔵 HOME JEFE (RESUMEN SIMPLE)
        public async Task<IActionResult> Jefe()
        {
            if (HttpContext.Session.GetString("ROL") != "JEFE")
                return RedirectToAction("Index", "Login");

            var peruTimeZone = TimeZoneInfo.FindSystemTimeZoneById(
                "SA Pacific Standard Time"
            );

            var ahoraPeru = TimeZoneInfo.ConvertTimeFromUtc(
                DateTime.UtcNow,
                peruTimeZone
            );

            var inicioDia = ahoraPeru.Date;
            var finDia = inicioDia.AddDays(1).AddTicks(-1);

            // =========================
            // 🔹 RESUMEN DEL DÍA
            // =========================

            var ventasHoy = _context.Ventas
                .Where(v => v.FechaRegistro >= inicioDia &&
                            v.FechaRegistro <= finDia)
                .ToList();

            ViewBag.VentasHoy = ventasHoy.Sum(v => v.Precio);
            ViewBag.CantidadVentas = ventasHoy.Count();

            var ultimaVentaUtc = _context.Ventas
                .OrderByDescending(v => v.FechaRegistro)
                .Select(v => v.FechaRegistro)
                .FirstOrDefault();

            DateTime? ultimaVentaPeru = null;

            if (ultimaVentaUtc != DateTime.MinValue)
            {
                ultimaVentaPeru = TimeZoneInfo.ConvertTimeFromUtc(
                    DateTime.SpecifyKind(ultimaVentaUtc, DateTimeKind.Utc),
                    peruTimeZone
                );
            }

            ViewBag.UltimaVenta = ultimaVentaPeru;

            // =========================
            // 🔹 RESUMEN SEMANA (LUN-VIE)
            // =========================

            int diff = (7 + (ahoraPeru.DayOfWeek - DayOfWeek.Monday)) % 7;
            var lunes = ahoraPeru.Date.AddDays(-diff);
            var viernes = lunes.AddDays(4);

            var inicioSemana = lunes;
            var finSemana = viernes.AddDays(1).AddTicks(-1);

            // Gastos semana
            ViewBag.GastosSemana = _context.Gastos
                .Where(g => g.FechaRegistro >= inicioSemana &&
                            g.FechaRegistro <= finSemana)
                .Sum(g => (decimal?)g.Total) ?? 0;

            // Ventas Yape semana
            ViewBag.YapeSemana = _context.Ventas
                .Where(v => v.FechaRegistro >= inicioSemana &&
                            v.FechaRegistro <= finSemana &&
                            v.Destino == "YAPE")
                .Sum(v => (decimal?)v.Precio) ?? 0;

            // Ventas Efectivo semana
            ViewBag.EfectivoSemana = _context.Ventas
                .Where(v => v.FechaRegistro >= inicioSemana &&
                            v.FechaRegistro <= finSemana &&
                            v.Destino == "EFECTIVO")
                .Sum(v => (decimal?)v.Precio) ?? 0;

            // =========================================================================
            // 🔥 INTEGRACIÓN: CARGAR INYECCIÓN DE MÉTRICAS GLOBALES Y ALERTAS
            // =========================================================================
            await CargarMetricasYAlertasSemanales(ahoraPeru);

            return View();
        }

        // =========================================================================
        // 🧠 MÉTODOS DE SOPORTE ASÍNCRONOS PARA LAS BOLSAS GLOBALES Y NOTIFICACIONES
        // =========================================================================
        private async Task CargarMetricasYAlertasSemanales(DateTime ahoraPeru)
        {
            // 1. Bolsas acumuladas globales totales
            ViewBag.SueldoAcumuladoTotal = await _context.PeriodosSemanales.SumAsync(p => p.SueldoSaldoPendiente);
            ViewBag.CajaAcumuladaTotal = await _context.PeriodosSemanales.SumAsync(p => p.CajaSaldoPendiente);

            // 2. Cargar las alertas activas del Dashboard (Máximo 4 y excluyendo el registro fantasma)
            ViewBag.AlertasSemanales = await _context.PeriodosSemanales
                .Where(p => p.EstadoMiSueldo == "PENDIENTE" || p.EstadoMiSueldo == "PARCIAL" ||
                            p.EstadoCajaJefe == "PENDIENTE" || p.EstadoCajaJefe == "PARCIAL")
                .Where(p => p.FechaInicio > new DateTime(2000, 1, 1))
                .OrderBy(p => p.FechaInicio)
                .Take(4)
                .ToListAsync();

            // 3. Contar la asistencia automática en vivo (Agrupado de forma compatible con EF)
            DateTime lunesActual = ObtenerLunesDeSemana(ahoraPeru);
            DateTime sabadoActual = lunesActual.AddDays(5).AddDays(1).AddSeconds(-1);

            var diasConGastos = await _context.Gastos
                .Where(g => g.FechaRegistro >= lunesActual && g.FechaRegistro <= sabadoActual)
                .GroupBy(g => g.FechaRegistro.Date)
                .CountAsync();

            int diasTrabajadosHoy = Math.Min(diasConGastos, 6);

            if (diasTrabajadosHoy == 0)
            {
                var diasConVentas = await _context.Ventas
                    .Where(v => v.FechaRegistro >= lunesActual && v.FechaRegistro <= sabadoActual)
                    .GroupBy(v => v.FechaRegistro.Date)
                    .CountAsync();
                diasTrabajadosHoy = Math.Min(diasConVentas, 6);
            }

            ViewBag.DiasTrabajadosEnCurso = diasTrabajadosHoy;
            ViewBag.RangoSemanaActualTexto = $"{lunesActual:dd/MM} al {lunesActual.AddDays(5):dd/MM}";
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            });
        }
    }
}