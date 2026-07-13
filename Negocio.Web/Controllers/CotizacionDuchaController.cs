using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Negocio.Web.Data;
using Negocio.Web.Models.ViewModels;

namespace Negocio.Web.Controllers
{
    public class CotizacionDuchaController : Controller
    {
        private readonly NegocioDbContext _context;

        public CotizacionDuchaController(NegocioDbContext context)
        {
            _context = context;
        }

        // ================================
        // PANTALLA PRINCIPAL
        // ================================
        public IActionResult Index()
        {
            return View();
        }

        // ================================
        // LISTAR TUBOS DUCHA / TOALLERA
        // ================================
        [HttpGet]
        public async Task<IActionResult> GetTubosDucha(string seccion, string material)
        {
            seccion = string.IsNullOrEmpty(seccion) ? "DUCHA" : seccion.ToUpper();

            var query = _context.Productos
                .AsNoTracking()
                .Where(p =>
                    p.Activo &&
                    p.Area == material &&
                    p.Descripcion.StartsWith("TUBO") &&
                    !p.Descripcion.Contains("PESADO") &&
                    !p.Descripcion.Contains("CUADRADO")
                

                // 🔴 SI QUIERES EXCLUIR UNA MEDIDA INGRESA AHÍ
                // Ejemplo:
                // && p.Dimensiones != "5/8"
                );

            // 🔹 TOALLERA → Solo 3/4
            if (seccion?.ToUpper() == "TOALLERA")
            {
                var medidasPermitidas = new[] { "3/4", "1/2", "5/8"};

                query = query.Where(p =>
                    medidasPermitidas.Contains(p.Dimensiones)
                );
            }

            // 🔹 CORTINA → Solo 3/4 y 1 PULGADA
            else if (seccion?.ToUpper() == "DUCHA")
            {
                var diametrosPermitidos = new[] { "7/8", "1 PULGADA", "3/4", "5/9" };

                query = query.Where(p =>
                    diametrosPermitidos.Contains(p.Dimensiones)
                );
            }

            var tubos = await query
                .OrderBy(p => p.Dimensiones)
                .Select(p => new
                {
                    idProducto = p.IdProducto,
                    descripcion = p.Descripcion,
                    dimension = p.Dimensiones,
                    precio = p.PrecioVenta,
                    unidad = p.Unidad,
                    imagen = p.ImagenUrl
                })
                .ToListAsync();

            return Json(tubos);
        }



        [HttpGet]
        public async Task<IActionResult> GetAccesoriosDucha(string seccion, string dimension)
        {
            var query = _context.Productos
                .AsNoTracking()
                .Where(p => p.Activo && p.Area == "ACCESORIOS");

            seccion = seccion?.ToUpper();

            if (seccion == "DUCHA")
            {
                query = query.Where(p =>
                    p.Descripcion != null &&
                    (
                        p.Descripcion.ToUpper().Contains("BRIDAS") ||
                        p.Descripcion.ToUpper().Contains("BRIDA AVIERTA")
                    )
                );
            }
            else if (seccion == "TOALLERA")
            {
                query = query.Where(p =>
                    p.Descripcion != null &&
                    (
                        p.Descripcion.ToUpper().Contains("PASANTES") ||
                        p.Descripcion.ToUpper().Contains("TERMINALES")
                    )
                );
            }

            // 🔥 FILTRO REAL POR DIMENSIÓN DEL TUBO
            if (!string.IsNullOrEmpty(dimension))
            {
                query = query.Where(p =>
                    p.Dimensiones != null &&
                    p.Dimensiones.Contains(dimension)
                );
            }

            var accesorios = await query
                .OrderBy(p => p.Descripcion)
                .Select(p => new
                {
                    idProducto = p.IdProducto,
                    descripcion = p.Descripcion,
                    dimension = p.Dimensiones,
                    precio = p.PrecioVenta,
                    unidad = p.Unidad,
                    imagen = p.ImagenUrl
                })
                .ToListAsync();

            return Json(accesorios);
        }





        // ================================
        // CALCULAR COTIZACIÓN DUCHA
        // ================================
        [HttpPost]
        public async Task<IActionResult> CalcularDucha(
            [FromBody] CotizacionPasamanoVm req) // reutilizas el mismo VM
        {
            if (req == null || req.CantidadPiezas <= 0)
                return BadRequest("Datos inválidos");

            var tubo = await _context.Productos
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.IdProducto == req.PasamanoProductoId);

            if (tubo == null)
                return NotFound("Tubo no encontrado");

            decimal metrosTotales = 0;
            decimal subtotalTubo = 0;
            decimal precioUnitario = 0;

            if (req.TipoVenta == "VARILLA")
            {
                precioUnitario = tubo.PrecioVenta;
                subtotalTubo = req.CantidadPiezas * precioUnitario;
            }
            else if (req.TipoVenta == "METRO")
            {
                if (req.Metros <= 0 || tubo.ConversionFactor <= 0)
                    return BadRequest("Datos inválidos");

                precioUnitario = Math.Ceiling(
                    tubo.PrecioVenta / tubo.ConversionFactor
                );

                metrosTotales = req.Metros * req.CantidadPiezas;
                subtotalTubo = metrosTotales * precioUnitario;
            }
            else
            {
                return BadRequest("Tipo de venta inválido");
            }

            // ACCESORIOS
            decimal subtotalAcc = 0;
            var accesoriosDetalle = new List<object>();

            if (req.Accesorios != null && req.Accesorios.Any())
            {
                var ids = req.Accesorios.Select(a => a.ProductoId).ToList();

                var productosAcc = await _context.Productos
                    .AsNoTracking()
                    .Where(p => ids.Contains(p.IdProducto))
                    .ToListAsync();

                foreach (var a in req.Accesorios)
                {
                    var prod = productosAcc.First(p => p.IdProducto == a.ProductoId);
                    var sub = prod.PrecioVenta * a.Cantidad;
                    subtotalAcc += sub;

                    accesoriosDetalle.Add(new
                    {
                        descripcion = prod.Descripcion,
                        cantidad = a.Cantidad,
                        subtotal = sub
                    });
                }
            }

            return Json(new
            {
                tipoVenta = req.TipoVenta,
                tubo = tubo.Descripcion,
                precioUnitario,
                metrosTotales = req.TipoVenta == "METRO" ? metrosTotales : (decimal?)null,
                cantidadVarillas = req.TipoVenta == "VARILLA" ? req.CantidadPiezas : (int?)null,
                subtotalTubo,
                accesorios = accesoriosDetalle,
                subtotalAcc,
                total = subtotalTubo + subtotalAcc
            });
        }
    }
}
