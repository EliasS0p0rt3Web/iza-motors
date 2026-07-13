using Microsoft.EntityFrameworkCore;
using Negocio.Web.Data;
using Negocio.Web.Models.Entities;
using Negocio.Web.Models.ViewModels;

namespace Negocio.Web.Services
{
    public class RielCortinaService
    {
        private readonly NegocioDbContext _context;

        public RielCortinaService(NegocioDbContext context)
        {
            _context = context;
        }

        // ⚡ FIRMA ACTUALIZADA: Se agregaron fechaManual y diaManual al final
        public async Task<RielCortinaResultadoViewModel> RegistrarVentaRielAsync(
            decimal metros,
            string tipoCruce,
            bool accesorioAparte,
            string? tipoUnera,
            decimal cantidadUneraExtra,
            decimal precio,
            string destino,
            DateTime fechaManual, // 🔥 NUEVO
            string diaManual)     // 🔥 NUEVO
        {
            using var tx = await _context.Database.BeginTransactionAsync();

            try
            {
                var resultado = new RielCortinaResultadoViewModel();

                // =============================
                // 1️⃣ BUSCAR PRODUCTOS
                // =============================

                var riel = await _context.Productos
                    .FirstOrDefaultAsync(p => p.Descripcion == "RIEL DE CORTINA");

                var uneraNormal = await _context.Productos
                    .FirstOrDefaultAsync(p => p.Descripcion == "UÑERA - NORMAL");

                var uneraTecho = await _context.Productos
                    .FirstOrDefaultAsync(p => p.Descripcion == "UÑERA - TECHO");

                var uneraPared = await _context.Productos
                    .FirstOrDefaultAsync(p => p.Descripcion == "UÑERA - PARED");

                var cruce = await _context.Productos
                    .FirstOrDefaultAsync(p => p.Descripcion == "CRUCE");

                var terminal = await _context.Productos
                    .FirstOrDefaultAsync(p => p.Descripcion == "TERMINAL");

                var ruedaTerminal = await _context.Productos
                    .FirstOrDefaultAsync(p => p.Descripcion == "RUEDA - TERMINAL");

                if (riel == null)
                    throw new Exception("No existe el producto RIEL BLANCO");

                // =============================
                // GUARDAR STOCK ANTES
                // =============================

                resultado.StockRielAntes = riel.StockActual;
                resultado.StockUneraAntes = uneraNormal?.StockActual ?? 0;
                resultado.StockCruceAntes = cruce?.StockActual ?? 0;
                resultado.StockTerminalAntes = terminal?.StockActual ?? 0;
                resultado.StockRuedaAntes = ruedaTerminal?.StockActual ?? 0;

                // =============================
                // 2️⃣ DESCONTAR RIEL
                // =============================

                decimal varillas = metros / riel.ConversionFactor;

                if (riel.StockActual < varillas)
                    throw new Exception("Stock insuficiente de riel.");

                riel.StockActual -= varillas;
                resultado.RielDescontado = varillas;

                // =============================
                // 3️⃣ UÑERA NORMAL AUTOMÁTICA
                // SOLO SI NO HAY ACCESORIO APARTE
                // =============================

                if (!accesorioAparte && uneraNormal != null)
                {
                    if (uneraNormal.StockActual < 1)
                        throw new Exception("Stock insuficiente de UÑERA NORMAL");

                    uneraNormal.StockActual -= 1;
                    resultado.UneraNormalDescontado = 1;
                }

                // =============================
                // 4️⃣ UÑERA EXTRA (OPCIONAL)
                // =============================

                if (accesorioAparte && cantidadUneraExtra > 0)
                {
                    Producto? uneraSeleccionada = null;

                    if (tipoUnera == "TECHO")
                        uneraSeleccionada = uneraTecho;

                    if (tipoUnera == "PARED")
                        uneraSeleccionada = uneraPared;

                    if (tipoUnera == "NORMAL")
                        uneraSeleccionada = uneraNormal;

                    if (uneraSeleccionada != null)
                    {
                        if (uneraSeleccionada.StockActual < cantidadUneraExtra)
                            throw new Exception("Stock insuficiente de UÑERA extra");

                        uneraSeleccionada.StockActual -= cantidadUneraExtra;

                        if (tipoUnera == "NORMAL")
                            resultado.UneraNormalDescontado = (int)cantidadUneraExtra;
                    }
                }

                // =============================
                // 5️⃣ CRUCE
                // =============================

                if (cruce != null)
                {
                    int cantidadCruce = tipoCruce == "AMBOS" ? 2 : 1;

                    if (cruce.StockActual < cantidadCruce)
                        throw new Exception("Stock insuficiente de CRUCE");

                    cruce.StockActual -= cantidadCruce;
                    resultado.CruceDescontado = cantidadCruce;
                }

                // =============================
                // 6️⃣ TERMINAL
                // =============================

                if (terminal != null)
                {
                    if (terminal.StockActual < 1)
                        throw new Exception("Stock insuficiente de TERMINAL");

                    terminal.StockActual -= 1;
                    resultado.TerminalDescontado = 1;
                }

                // =============================
                // 7️⃣ RUEDA TERMINAL
                // =============================

                if (ruedaTerminal != null)
                {
                    if (ruedaTerminal.StockActual < 1)
                        throw new Exception("Stock insuficiente de RUEDA TERMINAL");

                    ruedaTerminal.StockActual -= 1;
                    resultado.RuedaDescontado = 1;
                }

                // =============================
                // 8️⃣ REGISTRAR VENTA (Mapeo Corregido)
                // =============================

                var venta = new Venta
                {
                    FechaRegistro = fechaManual, // ⚡ Reemplazado DateTime.Today por la fecha del form
                    Dia = diaManual,             // ⚡ Reemplazado el DayOfWeek de sistema por el día del form
                    Area = "ARC",
                    Descripcion = "RIEL BLANCO",
                    Dimensiones = metros + " METROS",
                    Cantidad = metros,
                    Unidad = "METROS",
                    Precio = precio,
                    Destino = destino,
                    IdProducto = riel.IdProducto
                };

                _context.Ventas.Add(venta);
                await _context.SaveChangesAsync();

                // =============================
                // STOCK FINAL
                // =============================

                resultado.StockRiel = riel.StockActual;
                resultado.StockUnera = uneraNormal?.StockActual ?? 0;
                resultado.StockCruce = cruce?.StockActual ?? 0;
                resultado.StockTerminal = terminal?.StockActual ?? 0;
                resultado.StockRueda = ruedaTerminal?.StockActual ?? 0;

                await tx.CommitAsync();

                return resultado;
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }
    }
}