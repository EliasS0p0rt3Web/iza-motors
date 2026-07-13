using Negocio.Web.Data;
using Negocio.Web.Models.Entities;
using Negocio.Web.Models.ViewModels;

namespace Negocio.Web.Services
{
    public class TornilloService
    {
        private readonly NegocioDbContext _context;

        public TornilloService(NegocioDbContext context)
        {
            _context = context;
        }

        public async Task<string> RegistrarVentaAsync(VentaTornilloViewModel model)
        {
            using var tx = await _context.Database.BeginTransactionAsync();

            try
            {
                int unidadesReales = 0;
                decimal total = 0;

                if (model.TipoVenta == "DOCENA")
                {
                    unidadesReales = model.Cantidad * 12;
                    total = model.Cantidad * 2;
                }
                else
                {
                    unidadesReales = model.Cantidad;
                    total = model.Cantidad * (2m / 12m);
                }

                Producto? tornillo = null;
                Producto? tarubo = null;

                if (model.IdProductoTornillo > 0)
                {
                    tornillo = await _context.Productos.FindAsync(model.IdProductoTornillo);

                    if (tornillo == null)
                        return "Tornillo no encontrado";

                    if (tornillo.StockActual < unidadesReales)
                    {
                        return $"Falta stock en {tornillo.Descripcion} {tornillo.Dimensiones}. Stock disponible: {tornillo.StockActual}";
                    }

                    tornillo.StockActual -= unidadesReales;
                }

                if (model.IdProductoTarubo > 0)
                {
                    tarubo = await _context.Productos.FindAsync(model.IdProductoTarubo);

                    if (tarubo == null)
                        return "Tarubo no encontrado";

                    if (tarubo.StockActual < unidadesReales)
                    {
                        return $"Falta stock en {tarubo.Descripcion} {tarubo.Dimensiones}. Stock disponible: {tarubo.StockActual}";
                    }

                    tarubo.StockActual -= unidadesReales;
                }

                // =============================
                // 8️⃣ REGISTRAR VENTA (Actualizado con fecha manual)
                // =============================
                var venta = new Venta
                {
                    FechaRegistro = model.Fecha, // ⚡ Cambiado DateTime.Now por el del formulario
                    Dia = model.Dia,             // ⚡ Cambiado DateTime.Now por el del formulario

                    Area = "ACCESORIOS",
                    Descripcion = $"{tornillo?.Descripcion} {tornillo?.Dimensiones} + {tarubo?.Descripcion} {tarubo?.Dimensiones}",
                    Dimensiones = model.TipoVenta,
                    Cantidad = model.Cantidad,
                    Unidad = model.TipoVenta,
                    Precio = total,
                    Destino = model.Destino,
                    IdProducto = tornillo?.IdProducto ?? tarubo?.IdProducto
                };

                _context.Ventas.Add(venta);

                await _context.SaveChangesAsync();

                await tx.CommitAsync();

                return "OK";
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }
    }
}