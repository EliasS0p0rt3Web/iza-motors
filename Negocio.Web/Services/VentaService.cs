using Microsoft.EntityFrameworkCore;
using Negocio.Web.Data;
using Negocio.Web.Models.Entities;

namespace Negocio.Web.Services
{
    public class VentaService
    {
        private readonly NegocioDbContext _context;

        public VentaService(NegocioDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Registra una venta y actualiza stock
        /// </summary>
        public async Task RegistrarVentaAsync(Venta venta, bool actualizarStock)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // 🔹 CAMPOS OBLIGATORIOS DEL SISTEMA
                venta.FechaRegistro = DateTime.Today;
                venta.Dia = DateTime.Today.DayOfWeek.ToString().ToUpper();

                if (actualizarStock && venta.IdProducto.HasValue)
                {
                    var producto = await _context.Productos
                        .FirstOrDefaultAsync(p => p.IdProducto == venta.IdProducto.Value);

                    if (producto == null)
                        throw new Exception("Producto no encontrado.");

                    decimal cantidadEnVarillas = ConvertirAVarillas(
                        venta.Cantidad,
                        venta.Unidad,
                        producto.ConversionFactor
                    );

                    if (producto.StockActual < cantidadEnVarillas)
                        throw new Exception("Stock insuficiente.");

                    producto.StockActual -= cantidadEnVarillas;
                }

                // 🔹 GUARDAR VENTA
                _context.Ventas.Add(venta);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        private decimal ConvertirAVarillas(decimal cantidad, string unidad, decimal conversionFactor)
        {
            if (unidad.Trim().ToUpper() == "METROS")
            {
                if (conversionFactor <= 0)
                    throw new Exception("Factor de conversión inválido.");

                return cantidad / conversionFactor;
            }

            return cantidad;
        }
    }
}
