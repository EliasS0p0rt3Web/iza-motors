using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Negocio.Web.Data;
using Negocio.Web.Models.Entities;
using Negocio.Web.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace Negocio.Web.Controllers
{
    public class PeriodoSemanalController : Controller
    {
        private readonly NegocioDbContext _context;

        public PeriodoSemanalController(NegocioDbContext context)
        {
            _context = context;
        }

        // =========================================================
        // VISTA PRINCIPAL
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            DateTime ahoraPeru = ObtenerAhoraPeru();

            /*
             * IMPORTANTE:
             * Cada registro real aparece como una fila independiente.
             *
             * Ya no agrupamos varios IDs dentro de una sola fila,
             * porque eso provocaba que la pantalla sumara varios saldos,
             * pero el botón modificara solamente el primer registro.
             */
            var periodos = await _context.PeriodosSemanales
                .AsNoTracking()
                .Where(p => p.FechaInicio > new DateTime(2000, 1, 1))
                .Include(p => p.Movimientos)
                .OrderByDescending(p => p.FechaInicio)
                .ThenByDescending(p => p.IdPeriodoSemanal)
                .ToListAsync();

            var modelo = new PeriodoSemanalIndexViewModel
            {
                /*
                 * Se mantienen los totales sobre todos los registros.
                 *
                 * Esto permite que un registro de apertura con fecha
                 * 01/01/2000 permanezca oculto en el historial, pero
                 * pueda seguir formando parte del saldo acumulado.
                 */
                SueldoAcumuladoTotal =
                    await _context.PeriodosSemanales
                        .AsNoTracking()
                        .SumAsync(p => (decimal?)p.SueldoSaldoPendiente) ?? 0m,

                CajaAcumuladaTotal =
                    await _context.PeriodosSemanales
                        .AsNoTracking()
                        .SumAsync(p => (decimal?)p.CajaSaldoPendiente) ?? 0m,

                Periodos = periodos
                    .Select(p => new PeriodoSemanalFilaViewModel
                    {
                        IdPeriodoSemanal = p.IdPeriodoSemanal,
                        FechaInicio = p.FechaInicio,
                        FechaFin = p.FechaFin,
                        EfectivoGenerado = p.EfectivoGenerado,
                        YapeGenerado = p.YapeGenerado,
                        DiasTrabajados = p.DiasTrabajados,
                        SueldoCalculado = p.SueldoCalculado,
                        CajaSaldoPendiente = p.CajaSaldoPendiente,
                        EstadoCajaJefe = p.EstadoCajaJefe,
                        SueldoSaldoPendiente = p.SueldoSaldoPendiente,
                        EstadoMiSueldo = p.EstadoMiSueldo,
                        UltimaFechaModificacion = p.UltimaFechaModificacion,
                        Observaciones = p.Observaciones,
                        RegistroMetodoPago = p.RegistroMetodoPago,

                        Movimientos = p.Movimientos
                            .OrderByDescending(m => m.FechaRegistro)
                            .Select(m => new MovimientoPeriodoFilaViewModel
                            {
                                IdMovimientoPeriodoSemanal =
                                    m.IdMovimientoPeriodoSemanal,

                                TipoMovimiento = m.TipoMovimiento,
                                Monto = m.Monto,
                                MetodoPago = m.MetodoPago,
                                Observaciones = m.Observaciones,
                                FechaRegistro = m.FechaRegistro,
                                UsuarioRegistro = m.UsuarioRegistro
                            })
                            .ToList()
                    })
                    .ToList()
            };

            modelo.PeriodosPendientes =
                GenerarPeriodosPendientes(periodos, ahoraPeru);

            return View(modelo);
        }

        // =========================================================
        // CERRAR EL PRÓXIMO PERIODO DISPONIBLE
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CerrarSemanaActual(
            CerrarPeriodoRequest request)
        {
            if (!ModelState.IsValid)
            {
                return Json(
                    OperacionPeriodoResponse.Error(
                        ObtenerPrimerErrorModelo()
                    )
                );
            }

            if (!TryParseFecha(
                    request.FechaInicio,
                    "dd/MM/yyyy",
                    out DateTime fechaInicioSolicitada))
            {
                return Json(
                    OperacionPeriodoResponse.Error(
                        "La fecha inicial no tiene un formato válido."
                    )
                );
            }

            if (!TryParseFecha(
                    request.FechaFin,
                    "dd/MM/yyyy",
                    out DateTime fechaFinSolicitada))
            {
                return Json(
                    OperacionPeriodoResponse.Error(
                        "La fecha final no tiene un formato válido."
                    )
                );
            }

            DateTime ahoraPeru = ObtenerAhoraPeru();

            if (fechaInicioSolicitada.Date > fechaFinSolicitada.Date)
            {
                return Json(
                    OperacionPeriodoResponse.Error(
                        "La fecha de inicio no puede ser mayor que la fecha final."
                    )
                );
            }

            if (fechaFinSolicitada.Date > ahoraPeru.Date)
            {
                return Json(
                    OperacionPeriodoResponse.Error(
                        "No se puede cerrar un periodo con fechas futuras."
                    )
                );
            }

            try
            {
                /*
                 * SERIALIZABLE evita que dos solicitudes simultáneas
                 * lean el mismo último periodo e inserten dos cierres
                 * incompatibles.
                 */
                await using var transaccion =
                    await _context.Database.BeginTransactionAsync(
                        IsolationLevel.Serializable
                    );

                var ultimoPeriodo = await _context.PeriodosSemanales
                    .Where(p => p.FechaInicio > new DateTime(2000, 1, 1))
                    .OrderByDescending(p => p.FechaFin)
                    .ThenByDescending(p => p.IdPeriodoSemanal)
                    .FirstOrDefaultAsync();

                /*
                 * El siguiente periodo comienza exactamente después
                 * del final del anterior.
                 *
                 * Esto permite continuar correctamente incluso si el
                 * periodo anterior terminó en medio del día.
                 */
                DateTime inicioReal = ultimoPeriodo != null
                    ? ultimoPeriodo.FechaFin.AddTicks(1)
                    : ObtenerLunesDeSemana(ahoraPeru);

                /*
                 * Solo se permite cerrar el tramo pendiente más antiguo.
                 * Así el usuario no puede saltarse semanas anteriores.
                 */
                if (fechaInicioSolicitada.Date != inicioReal.Date)
                {
                    return Json(
                        OperacionPeriodoResponse.Error(
                            $"Primero debes cerrar el periodo pendiente que comienza el {inicioReal:dd/MM/yyyy}."
                        )
                    );
                }

                /*
                 * Un periodo normal tendrá como máximo siete días
                 * calendario desde su fecha inicial.
                 */
                DateTime fechaMaximaPermitida =
                    inicioReal.Date.AddDays(6);

                if (fechaFinSolicitada.Date > fechaMaximaPermitida.Date)
                {
                    return Json(
                        OperacionPeriodoResponse.Error(
                            "Un cierre normal no puede superar siete días calendario."
                        )
                    );
                }

                /*
                 * Si termina hoy, cerramos hasta la hora actual.
                 * Si es un día anterior, cerramos al último tick del día.
                 */
                DateTime finReal = fechaFinSolicitada.Date == ahoraPeru.Date
                    ? ahoraPeru
                    : fechaFinSolicitada.Date.AddDays(1).AddTicks(-1);

                if (inicioReal > finReal)
                {
                    return Json(
                        OperacionPeriodoResponse.Error(
                            "El periodo solicitado no contiene tiempo pendiente por cerrar."
                        )
                    );
                }

                bool existeCruce =
    await ExisteCruceDePeriodos(inicioReal, finReal);

                if (existeCruce)
                {
                    return Json(
                        OperacionPeriodoResponse.Error(
                            "El rango solicitado se cruza con un periodo ya cerrado."
                        )
                    );
                }

                // ✅ NUEVA VALIDACIÓN:
                // No guardar cortes vacíos sin ventas ni gastos.
                bool existeActividad =
                    await ExisteActividadContable(inicioReal, finReal);

                if (!existeActividad)
                {
                    return Json(
                        OperacionPeriodoResponse.Error(
                            "No existen ventas ni gastos nuevos desde el último cierre. " +
                            "El periodo permanecerá abierto hasta que se registre actividad."
                        )
                    );
                }

                var nuevoPeriodo =
                    await ConstruirPeriodoSemanal(
                        inicioReal,
                        finReal,
                        false
                    );

                _context.PeriodosSemanales.Add(nuevoPeriodo);

                await _context.SaveChangesAsync();
                await transaccion.CommitAsync();

                return Json(
                    OperacionPeriodoResponse.Exito(
                        $"Corte procesado correctamente. Periodo: " +
                        $"{nuevoPeriodo.FechaInicio:dd/MM/yyyy HH:mm} al " +
                        $"{nuevoPeriodo.FechaFin:dd/MM/yyyy HH:mm}. " +
                        $"Sueldo calculado: S/ {nuevoPeriodo.SueldoCalculado:F2}. " +
                        $"Caja neta: S/ {nuevoPeriodo.CajaSaldoPendiente:F2}."
                    )
                );
            }
            catch (DbUpdateConcurrencyException)
            {
                return Json(
                    OperacionPeriodoResponse.Error(
                        "Otro usuario modificó la información al mismo tiempo. Recarga la página e inténtalo nuevamente."
                    )
                );
            }
            catch (Exception)
            {
                return Json(
                    OperacionPeriodoResponse.Error(
                        "No se pudo cerrar el periodo. Revisa los datos e inténtalo nuevamente."
                    )
                );
            }
        }

        // =========================================================
        // CIERRE ESPECIAL POR RANGO MANUAL
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CerrarPorRangoManual(
            CerrarPeriodoManualRequest request)
        {
            if (!ModelState.IsValid)
            {
                return Json(
                    OperacionPeriodoResponse.Error(
                        ObtenerPrimerErrorModelo()
                    )
                );
            }

            if (!TryParseFecha(
                    request.FechaInicio,
                    "yyyy-MM-dd",
                    out DateTime fechaInicio))
            {
                return Json(
                    OperacionPeriodoResponse.Error(
                        "La fecha inicial no tiene un formato válido."
                    )
                );
            }

            if (!TryParseFecha(
                    request.FechaFin,
                    "yyyy-MM-dd",
                    out DateTime fechaFin))
            {
                return Json(
                    OperacionPeriodoResponse.Error(
                        "La fecha final no tiene un formato válido."
                    )
                );
            }

            DateTime ahoraPeru = ObtenerAhoraPeru();

            DateTime inicioReal = fechaInicio.Date;
            DateTime finReal = fechaFin.Date == ahoraPeru.Date
                ? ahoraPeru
                : fechaFin.Date.AddDays(1).AddTicks(-1);

            if (inicioReal > finReal)
            {
                return Json(
                    OperacionPeriodoResponse.Error(
                        "La fecha de inicio no puede ser mayor que la fecha final."
                    )
                );
            }

            if (inicioReal.Date > ahoraPeru.Date ||
                finReal.Date > ahoraPeru.Date)
            {
                return Json(
                    OperacionPeriodoResponse.Error(
                        "No se pueden cerrar periodos con fechas futuras."
                    )
                );
            }

            /*
             * El cierre especial no debería convertirse en un reporte
             * gigantesco. Se limita a 31 días para evitar errores
             * accidentales y cálculos excesivos.
             */
            if ((finReal.Date - inicioReal.Date).TotalDays > 30)
            {
                return Json(
                    OperacionPeriodoResponse.Error(
                        "El cierre especial no puede superar 31 días."
                    )
                );
            }

            try
            {
                await using var transaccion =
                    await _context.Database.BeginTransactionAsync(
                        IsolationLevel.Serializable
                    );

                bool existeCruce =
    await ExisteCruceDePeriodos(inicioReal, finReal);

                if (existeCruce)
                {
                    return Json(
                        OperacionPeriodoResponse.Error(
                            "El rango seleccionado se cruza con un periodo ya cerrado. " +
                            "Selecciona únicamente fechas todavía no contabilizadas."
                        )
                    );
                }

                bool existeActividad =
                    await ExisteActividadContable(inicioReal, finReal);

                if (!existeActividad)
                {
                    return Json(
                        OperacionPeriodoResponse.Error(
                            "No existen ventas ni gastos en el rango seleccionado. " +
                            "No se puede crear un cierre contable vacío."
                        )
                    );
                }

                var nuevoPeriodo =
                    await ConstruirPeriodoSemanal(
                        inicioReal,
                        finReal,
                        true
                    );

                _context.PeriodosSemanales.Add(nuevoPeriodo);

                await _context.SaveChangesAsync();
                await transaccion.CommitAsync();

                return Json(
                    OperacionPeriodoResponse.Exito(
                        $"Cierre especial procesado correctamente. " +
                        $"Rango: {nuevoPeriodo.FechaInicio:dd/MM/yyyy} al " +
                        $"{nuevoPeriodo.FechaFin:dd/MM/yyyy}."
                    )
                );
            }
            catch (DbUpdateConcurrencyException)
            {
                return Json(
                    OperacionPeriodoResponse.Error(
                        "La información cambió mientras se realizaba el cierre. Recarga la página e inténtalo nuevamente."
                    )
                );
            }
            catch (Exception)
            {
                return Json(
                    OperacionPeriodoResponse.Error(
                        "No se pudo completar el cierre especial."
                    )
                );
            }
        }

        // =========================================================
        // PAGO LIBRE DE SUELDO EN CASCADA
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegistrarPagoLibre(
            RegistrarPagoLibreRequest request)
        {
            if (!ModelState.IsValid)
            {
                return Json(
                    OperacionPeriodoResponse.Error(
                        ObtenerPrimerErrorModelo()
                    )
                );
            }

            string metodoPago =
                NormalizarMetodoPago(request.MetodoPago);

            if (!EsMetodoPagoValido(metodoPago))
            {
                return Json(
                    OperacionPeriodoResponse.Error(
                        "El método de pago seleccionado no es válido."
                    )
                );
            }

            try
            {
                await using var transaccion =
                    await _context.Database.BeginTransactionAsync(
                        IsolationLevel.Serializable
                    );

                var periodosPendientes =
                    await _context.PeriodosSemanales
                        .Where(p =>
                            p.SueldoSaldoPendiente > 0 &&
                            (
                                p.EstadoMiSueldo == "PENDIENTE" ||
                                p.EstadoMiSueldo == "PARCIAL"
                            )
                        )
                        .OrderBy(p => p.FechaInicio)
                        .ThenBy(p => p.IdPeriodoSemanal)
                        .ToListAsync();

                if (periodosPendientes.Count == 0)
                {
                    return Json(
                        OperacionPeriodoResponse.Error(
                            "No existen periodos con sueldo pendiente."
                        )
                    );
                }

                decimal montoPendiente = request.Monto;
                decimal montoAplicadoTotal = 0m;
                DateTime fechaMovimiento = ObtenerAhoraPeru();
                string? usuario = ObtenerUsuarioActual();

                foreach (var periodo in periodosPendientes)
                {
                    if (montoPendiente <= 0)
                    {
                        break;
                    }

                    decimal montoAplicado =
                        Math.Min(
                            montoPendiente,
                            periodo.SueldoSaldoPendiente
                        );

                    if (montoAplicado <= 0)
                    {
                        continue;
                    }

                    periodo.SueldoSaldoPendiente -= montoAplicado;
                    periodo.SueldoSaldoPendiente =
                        Math.Max(0m, periodo.SueldoSaldoPendiente);

                    periodo.EstadoMiSueldo =
                        periodo.SueldoSaldoPendiente == 0m
                            ? "PAGADO"
                            : "PARCIAL";

                    periodo.RegistroMetodoPago = metodoPago;
                    periodo.UltimaFechaModificacion = fechaMovimiento;

                    _context.MovimientosPeriodoSemanal.Add(
                        new MovimientoPeriodoSemanal
                        {
                            IdPeriodoSemanal =
                                periodo.IdPeriodoSemanal,

                            TipoMovimiento = "PAGO_SUELDO",
                            Monto = montoAplicado,
                            MetodoPago = metodoPago,
                            Observaciones =
                                LimpiarTexto(request.Observaciones, 500),

                            FechaRegistro = fechaMovimiento,
                            UsuarioRegistro = usuario
                        }
                    );

                    montoPendiente -= montoAplicado;
                    montoAplicadoTotal += montoAplicado;
                }

                await _context.SaveChangesAsync();
                await transaccion.CommitAsync();

                if (montoPendiente > 0)
                {
                    return Json(
                        OperacionPeriodoResponse.Exito(
                            $"Se aplicaron S/ {montoAplicadoTotal:F2} a la deuda. " +
                            $"El excedente de S/ {montoPendiente:F2} no fue descontado ni almacenado."
                        )
                    );
                }

                return Json(
                    OperacionPeriodoResponse.Exito(
                        $"Pago de S/ {montoAplicadoTotal:F2} registrado correctamente en cascada."
                    )
                );
            }
            catch (DbUpdateConcurrencyException)
            {
                return Json(
                    OperacionPeriodoResponse.Error(
                        "Uno de los saldos fue modificado al mismo tiempo. Recarga la página y vuelve a registrar el pago."
                    )
                );
            }
            catch (Exception)
            {
                return Json(
                    OperacionPeriodoResponse.Error(
                        "No se pudo registrar el pago."
                    )
                );
            }
        }

        // =========================================================
        // RETIRO LIBRE DE CAJA EN CASCADA
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegistrarRetiroCajaLibre(
            RegistrarRetiroLibreRequest request)
        {
            if (!ModelState.IsValid)
            {
                return Json(
                    OperacionPeriodoResponse.Error(
                        ObtenerPrimerErrorModelo()
                    )
                );
            }

            try
            {
                await using var transaccion =
                    await _context.Database.BeginTransactionAsync(
                        IsolationLevel.Serializable
                    );

                var periodosPendientes =
                    await _context.PeriodosSemanales
                        .Where(p =>
                            p.CajaSaldoPendiente > 0 &&
                            (
                                p.EstadoCajaJefe == "PENDIENTE" ||
                                p.EstadoCajaJefe == "PARCIAL"
                            )
                        )
                        .OrderBy(p => p.FechaInicio)
                        .ThenBy(p => p.IdPeriodoSemanal)
                        .ToListAsync();

                if (periodosPendientes.Count == 0)
                {
                    return Json(
                        OperacionPeriodoResponse.Error(
                            "No existen periodos con saldo de caja pendiente."
                        )
                    );
                }

                decimal montoPendiente = request.Monto;
                decimal montoAplicadoTotal = 0m;
                DateTime fechaMovimiento = ObtenerAhoraPeru();
                string? usuario = ObtenerUsuarioActual();

                foreach (var periodo in periodosPendientes)
                {
                    if (montoPendiente <= 0)
                    {
                        break;
                    }

                    decimal montoAplicado =
                        Math.Min(
                            montoPendiente,
                            periodo.CajaSaldoPendiente
                        );

                    if (montoAplicado <= 0)
                    {
                        continue;
                    }

                    periodo.CajaSaldoPendiente -= montoAplicado;
                    periodo.CajaSaldoPendiente =
                        Math.Max(0m, periodo.CajaSaldoPendiente);

                    periodo.EstadoCajaJefe =
                        periodo.CajaSaldoPendiente == 0m
                            ? "RETIRADO"
                            : "PARCIAL";

                    periodo.UltimaFechaModificacion = fechaMovimiento;

                    _context.MovimientosPeriodoSemanal.Add(
                        new MovimientoPeriodoSemanal
                        {
                            IdPeriodoSemanal =
                                periodo.IdPeriodoSemanal,

                            TipoMovimiento = "RETIRO_CAJA",
                            Monto = montoAplicado,
                            MetodoPago = null,
                            Observaciones =
                                LimpiarTexto(request.Observaciones, 500),

                            FechaRegistro = fechaMovimiento,
                            UsuarioRegistro = usuario
                        }
                    );

                    montoPendiente -= montoAplicado;
                    montoAplicadoTotal += montoAplicado;
                }

                await _context.SaveChangesAsync();
                await transaccion.CommitAsync();

                if (montoPendiente > 0)
                {
                    return Json(
                        OperacionPeriodoResponse.Exito(
                            $"Se retiraron S/ {montoAplicadoTotal:F2}. " +
                            $"El excedente solicitado de S/ {montoPendiente:F2} no fue descontado."
                        )
                    );
                }

                return Json(
                    OperacionPeriodoResponse.Exito(
                        $"Retiro de S/ {montoAplicadoTotal:F2} registrado correctamente."
                    )
                );
            }
            catch (DbUpdateConcurrencyException)
            {
                return Json(
                    OperacionPeriodoResponse.Error(
                        "Uno de los saldos cambió al mismo tiempo. Recarga la página y vuelve a registrar el retiro."
                    )
                );
            }
            catch (Exception)
            {
                return Json(
                    OperacionPeriodoResponse.Error(
                        "No se pudo registrar el retiro de caja."
                    )
                );
            }
        }

        // =========================================================
        // PAGAR TODO EL SUELDO DE UN PERIODO ESPECÍFICO
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegistrarPagoCompleto(
            RegistrarPagoCompletoRequest request)
        {
            if (!ModelState.IsValid)
            {
                return Json(
                    OperacionPeriodoResponse.Error(
                        ObtenerPrimerErrorModelo()
                    )
                );
            }

            string metodoPago =
                NormalizarMetodoPago(request.MetodoPago);

            if (!EsMetodoPagoValido(metodoPago))
            {
                return Json(
                    OperacionPeriodoResponse.Error(
                        "El método de pago seleccionado no es válido."
                    )
                );
            }

            try
            {
                await using var transaccion =
                    await _context.Database.BeginTransactionAsync(
                        IsolationLevel.Serializable
                    );

                var periodo = await _context.PeriodosSemanales
                    .FirstOrDefaultAsync(p =>
                        p.IdPeriodoSemanal ==
                        request.IdPeriodoSemanal
                    );

                if (periodo == null)
                {
                    return Json(
                        OperacionPeriodoResponse.Error(
                            "No se encontró el periodo solicitado."
                        )
                    );
                }

                /*
                 * El monto no llega desde JavaScript.
                 * Se obtiene directamente del saldo real de SQL Server.
                 */
                decimal montoAplicado =
                    periodo.SueldoSaldoPendiente;

                if (montoAplicado <= 0)
                {
                    return Json(
                        OperacionPeriodoResponse.Error(
                            "El sueldo de este periodo ya se encuentra pagado."
                        )
                    );
                }

                DateTime fechaMovimiento = ObtenerAhoraPeru();

                periodo.SueldoSaldoPendiente = 0m;
                periodo.EstadoMiSueldo = "PAGADO";
                periodo.RegistroMetodoPago = metodoPago;
                periodo.UltimaFechaModificacion = fechaMovimiento;

                _context.MovimientosPeriodoSemanal.Add(
                    new MovimientoPeriodoSemanal
                    {
                        IdPeriodoSemanal =
                            periodo.IdPeriodoSemanal,

                        TipoMovimiento = "PAGO_SUELDO",
                        Monto = montoAplicado,
                        MetodoPago = metodoPago,
                        Observaciones =
                            LimpiarTexto(request.Observaciones, 500),

                        FechaRegistro = fechaMovimiento,
                        UsuarioRegistro = ObtenerUsuarioActual()
                    }
                );

                await _context.SaveChangesAsync();
                await transaccion.CommitAsync();

                return Json(
                    OperacionPeriodoResponse.Exito(
                        $"Sueldo completo de S/ {montoAplicado:F2} registrado correctamente."
                    )
                );
            }
            catch (DbUpdateConcurrencyException)
            {
                return Json(
                    OperacionPeriodoResponse.Error(
                        "El saldo fue modificado por otra operación. Recarga la página."
                    )
                );
            }
            catch (Exception)
            {
                return Json(
                    OperacionPeriodoResponse.Error(
                        "No se pudo registrar el pago completo."
                    )
                );
            }
        }

        // =========================================================
        // RETIRAR TODA LA CAJA DE UN PERIODO ESPECÍFICO
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegistrarRetiroCompleto(
            RegistrarRetiroCompletoRequest request)
        {
            if (!ModelState.IsValid)
            {
                return Json(
                    OperacionPeriodoResponse.Error(
                        ObtenerPrimerErrorModelo()
                    )
                );
            }

            try
            {
                await using var transaccion =
                    await _context.Database.BeginTransactionAsync(
                        IsolationLevel.Serializable
                    );

                var periodo = await _context.PeriodosSemanales
                    .FirstOrDefaultAsync(p =>
                        p.IdPeriodoSemanal ==
                        request.IdPeriodoSemanal
                    );

                if (periodo == null)
                {
                    return Json(
                        OperacionPeriodoResponse.Error(
                            "No se encontró el periodo solicitado."
                        )
                    );
                }

                /*
                 * El monto se obtiene del saldo real de la base.
                 * El navegador únicamente envía el ID.
                 */
                decimal montoAplicado =
                    periodo.CajaSaldoPendiente;

                if (montoAplicado <= 0)
                {
                    return Json(
                        OperacionPeriodoResponse.Error(
                            "La caja de este periodo ya fue retirada."
                        )
                    );
                }

                DateTime fechaMovimiento = ObtenerAhoraPeru();

                periodo.CajaSaldoPendiente = 0m;
                periodo.EstadoCajaJefe = "RETIRADO";
                periodo.UltimaFechaModificacion = fechaMovimiento;

                _context.MovimientosPeriodoSemanal.Add(
                    new MovimientoPeriodoSemanal
                    {
                        IdPeriodoSemanal =
                            periodo.IdPeriodoSemanal,

                        TipoMovimiento = "RETIRO_CAJA",
                        Monto = montoAplicado,
                        MetodoPago = null,
                        Observaciones =
                            LimpiarTexto(request.Observaciones, 500),

                        FechaRegistro = fechaMovimiento,
                        UsuarioRegistro = ObtenerUsuarioActual()
                    }
                );

                await _context.SaveChangesAsync();
                await transaccion.CommitAsync();

                return Json(
                    OperacionPeriodoResponse.Exito(
                        $"Retiro completo de S/ {montoAplicado:F2} registrado correctamente."
                    )
                );
            }
            catch (DbUpdateConcurrencyException)
            {
                return Json(
                    OperacionPeriodoResponse.Error(
                        "El saldo fue modificado por otra operación. Recarga la página."
                    )
                );
            }
            catch (Exception)
            {
                return Json(
                    OperacionPeriodoResponse.Error(
                        "No se pudo registrar el retiro completo."
                    )
                );
            }
        }

        // =========================================================
        // CONSULTAR EL HISTORIAL REAL DE UN PERIODO
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> ObtenerMovimientos(int id)
        {
            if (id <= 0)
            {
                return Json(
                    OperacionPeriodoResponse.Error(
                        "El periodo solicitado no es válido."
                    )
                );
            }

            bool existePeriodo =
                await _context.PeriodosSemanales
                    .AsNoTracking()
                    .AnyAsync(p => p.IdPeriodoSemanal == id);

            if (!existePeriodo)
            {
                return Json(
                    OperacionPeriodoResponse.Error(
                        "No se encontró el periodo solicitado."
                    )
                );
            }

            var movimientos =
                await _context.MovimientosPeriodoSemanal
                    .AsNoTracking()
                    .Where(m => m.IdPeriodoSemanal == id)
                    .OrderByDescending(m => m.FechaRegistro)
                    .Select(m => new MovimientoPeriodoFilaViewModel
                    {
                        IdMovimientoPeriodoSemanal =
                            m.IdMovimientoPeriodoSemanal,

                        TipoMovimiento = m.TipoMovimiento,
                        Monto = m.Monto,
                        MetodoPago = m.MetodoPago,
                        Observaciones = m.Observaciones,
                        FechaRegistro = m.FechaRegistro,
                        UsuarioRegistro = m.UsuarioRegistro
                    })
                    .ToListAsync();

            return Json(
                OperacionPeriodoResponse.Exito(
                    "Historial cargado correctamente.",
                    movimientos
                )
            );
        }

        // =========================================================
        // CONSTRUIR UN NUEVO PERIODO DESDE VENTAS Y GASTOS
        // =========================================================
        private async Task<PeriodoSemanal> ConstruirPeriodoSemanal(
            DateTime inicio,
            DateTime fin,
            bool esCierreEspecial)
        {
            /*
             * Se respeta la misma lógica del ReporteController:
             *
             * EFECTIVO = ventas cuyo Destino es EFECTIVO.
             * YAPE     = ventas cuyo Destino es YAPE.
             * CAJA     = efectivo menos gastos.
             *
             * Los gastos no se descuentan de Yape.
             */
            decimal totalEfectivo =
                await _context.Ventas
                    .Where(v =>
                        v.FechaRegistro >= inicio &&
                        v.FechaRegistro <= fin &&
                        v.Destino == "EFECTIVO"
                    )
                    .SumAsync(v => (decimal?)v.Precio) ?? 0m;

            decimal totalYape =
                await _context.Ventas
                    .Where(v =>
                        v.FechaRegistro >= inicio &&
                        v.FechaRegistro <= fin &&
                        v.Destino == "YAPE"
                    )
                    .SumAsync(v => (decimal?)v.Precio) ?? 0m;

            decimal totalGastos =
                await _context.Gastos
                    .Where(g =>
                        g.FechaRegistro >= inicio &&
                        g.FechaRegistro <= fin
                    )
                    .SumAsync(g => (decimal?)g.Total) ?? 0m;

            /*
             * La asistencia se calcula uniendo días con ventas
             * y días con gastos.
             *
             * Antes solo se miraban ventas cuando no había gastos,
             * lo cual podía ignorar días realmente trabajados.
             */
            var diasConVentas = await _context.Ventas
                .Where(v =>
                    v.FechaRegistro >= inicio &&
                    v.FechaRegistro <= fin
                )
                .Select(v => v.FechaRegistro.Date)
                .Distinct()
                .ToListAsync();

            var diasConGastos = await _context.Gastos
                .Where(g =>
                    g.FechaRegistro >= inicio &&
                    g.FechaRegistro <= fin
                )
                .Select(g => g.FechaRegistro.Date)
                .Distinct()
                .ToListAsync();

            int diasTrabajados = diasConVentas
                .Concat(diasConGastos)
                .Distinct()
                .Count();

            diasTrabajados = Math.Min(diasTrabajados, 6);

            decimal sueldoPorDia = 200m / 6m;

            decimal sueldoCalculado =
                Math.Round(
                    diasTrabajados * sueldoPorDia,
                    2,
                    MidpointRounding.AwayFromZero
                );

            decimal cajaNeta =
                totalEfectivo - totalGastos;

            /*
             * Se mantiene la regla actual del negocio:
             * la caja pendiente nunca se guarda como negativa.
             */
            if (cajaNeta < 0m)
            {
                cajaNeta = 0m;
            }

            DateTime ahoraPeru = ObtenerAhoraPeru();

            return new PeriodoSemanal
            {
                FechaInicio = inicio,
                FechaFin = fin,

                /*
                 * EfectivoGenerado guarda el efectivo bruto.
                 * CajaSaldoPendiente guarda el efectivo neto
                 * después de descontar gastos.
                 */
                EfectivoGenerado = totalEfectivo,
                YapeGenerado = totalYape,

                DiasTrabajados = diasTrabajados,
                SueldoCalculado = sueldoCalculado,

                CajaSaldoPendiente = cajaNeta,
                EstadoCajaJefe =
                    cajaNeta > 0m
                        ? "PENDIENTE"
                        : "RETIRADO",

                SueldoSaldoPendiente = sueldoCalculado,
                EstadoMiSueldo =
                    sueldoCalculado > 0m
                        ? "PENDIENTE"
                        : "PAGADO",

                UltimaFechaModificacion = ahoraPeru,

                Observaciones = esCierreEspecial
                    ? $"Cierre especial del {inicio:dd/MM/yyyy} al {fin:dd/MM/yyyy}."
                    : $"Corte contable del {inicio:dd/MM/yyyy HH:mm} al {fin:dd/MM/yyyy HH:mm}.",

                RegistroMetodoPago = null
            };
        }

        // =========================================================
        // GENERAR PERIODOS PENDIENTES PARA EL COMBO
        // =========================================================
        private List<PeriodoPendienteViewModel>
            GenerarPeriodosPendientes(
                List<PeriodoSemanal> periodos,
                DateTime ahoraPeru)
        {
            var resultado =
                new List<PeriodoPendienteViewModel>();

            var ultimoPeriodo = periodos
                .OrderByDescending(p => p.FechaFin)
                .ThenByDescending(p => p.IdPeriodoSemanal)
                .FirstOrDefault();

            DateTime inicioPendiente =
                ultimoPeriodo != null
                    ? ultimoPeriodo.FechaFin.AddTicks(1)
                    : ObtenerLunesDeSemana(ahoraPeru);

            /*
             * Si el último cierre terminó hoy a una hora determinada,
             * el nuevo tramo puede comenzar hoy mismo desde el instante
             * siguiente. Así no se pierden ventas posteriores.
             */
            while (inicioPendiente <= ahoraPeru)
            {
                DateTime finMaximo =
                    inicioPendiente.Date
                        .AddDays(7)
                        .AddTicks(-1);

                DateTime finPendiente =
                    finMaximo > ahoraPeru
                        ? ahoraPeru
                        : finMaximo;

                resultado.Add(
                    new PeriodoPendienteViewModel
                    {
                        FechaInicio = inicioPendiente,
                        FechaFin = finPendiente
                    }
                );

                if (finPendiente >= ahoraPeru)
                {
                    break;
                }

                inicioPendiente = finPendiente.AddTicks(1);
            }

            return resultado;
        }

        // =========================================================
        // VALIDAR CRUCE ENTRE PERIODOS
        // =========================================================
        private async Task<bool> ExisteCruceDePeriodos(
            DateTime inicio,
            DateTime fin)
        {
            /*
             * Dos rangos se cruzan cuando:
             *
             * inicio existente <= fin nuevo
             * y
             * fin existente >= inicio nuevo
             */
            return await _context.PeriodosSemanales
                .AnyAsync(p =>
                    p.FechaInicio <= fin &&
                    p.FechaFin >= inicio
                );
        }

        // =========================================================
        // OBTENER EL LUNES DE LA SEMANA
        // =========================================================
        private static DateTime ObtenerLunesDeSemana(
            DateTime fecha)
        {
            int diferencia =
                (7 + (fecha.DayOfWeek - DayOfWeek.Monday)) % 7;

            return fecha.Date.AddDays(-diferencia);
        }

        // =========================================================
        // HORA ACTUAL DE PERÚ, WINDOWS Y LINUX
        // =========================================================
        private static DateTime ObtenerAhoraPeru()
        {
            TimeZoneInfo zonaPeru;

            try
            {
                /*
                 * Identificador usado normalmente en Windows.
                 */
                zonaPeru = TimeZoneInfo.FindSystemTimeZoneById(
                    "SA Pacific Standard Time"
                );
            }
            catch (TimeZoneNotFoundException)
            {
                /*
                 * Identificador usado normalmente en Linux.
                 */
                zonaPeru = TimeZoneInfo.FindSystemTimeZoneById(
                    "America/Lima"
                );
            }

            return TimeZoneInfo.ConvertTimeFromUtc(
                DateTime.UtcNow,
                zonaPeru
            );
        }

        // =========================================================
        // CONVERTIR FECHAS SIN LANZAR FORMATEXCEPTION
        // =========================================================
        private static bool TryParseFecha(
            string? texto,
            string formato,
            out DateTime fecha)
        {
            return DateTime.TryParseExact(
                texto,
                formato,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out fecha
            );
        }

        // =========================================================
        // PRIMER ERROR DE VALIDACIÓN
        // =========================================================
        private string ObtenerPrimerErrorModelo()
        {
            return ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .FirstOrDefault()
                ?? "Los datos enviados no son válidos.";
        }

        // =========================================================
        // MÉTODO DE PAGO
        // =========================================================
        private static string NormalizarMetodoPago(
            string? metodoPago)
        {
            return (metodoPago ?? string.Empty)
                .Trim()
                .ToUpperInvariant();
        }

        private static bool EsMetodoPagoValido(
            string metodoPago)
        {
            return metodoPago == "EFECTIVO" ||
                   metodoPago == "YAPE" ||
                   metodoPago == "TRANSFERENCIA";
        }

        // =========================================================
        // LIMPIAR Y LIMITAR TEXTO
        // =========================================================
        private static string? LimpiarTexto(
            string? texto,
            int longitudMaxima)
        {
            if (string.IsNullOrWhiteSpace(texto))
            {
                return null;
            }

            string resultado = texto.Trim();

            return resultado.Length <= longitudMaxima
                ? resultado
                : resultado.Substring(0, longitudMaxima);
        }

        // =========================================================
        // VALIDAR SI EXISTE ACTIVIDAD CONTABLE EN EL RANGO
        // =========================================================
        private async Task<bool> ExisteActividadContable(
            DateTime inicio,
            DateTime fin)
        {
            bool existenVentas = await _context.Ventas
                .AsNoTracking()
                .AnyAsync(v =>
                    v.FechaRegistro >= inicio &&
                    v.FechaRegistro <= fin
                );

            if (existenVentas)
            {
                return true;
            }

            bool existenGastos = await _context.Gastos
                .AsNoTracking()
                .AnyAsync(g =>
                    g.FechaRegistro >= inicio &&
                    g.FechaRegistro <= fin
                );

            return existenGastos;
        }

        // =========================================================
        // USUARIO ACTUAL
        // =========================================================
        private string? ObtenerUsuarioActual()
        {
            if (User?.Identity?.IsAuthenticated == true)
            {
                return LimpiarTexto(
                    User.Identity.Name,
                    150
                );
            }

            return null;
        }
    }
}

