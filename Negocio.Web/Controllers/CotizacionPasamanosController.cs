using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Negocio.Web.Data;
using Negocio.Web.Models.ViewModels;

namespace Negocio.Web.Controllers
{
    public class CotizacionPasamanosController : Controller
    {
        private readonly NegocioDbContext _context;

        public CotizacionPasamanosController(NegocioDbContext context)
        {
            _context = context;
        }

        // ================================
        // PANTALLA PASAMANOS
        // ================================
        public IActionResult Index()
        {
            return View();
        }

        // ================================
        // LISTAR TUBOS PASAMANOS
        // ================================
        [HttpGet]
        public async Task<IActionResult> GetTubosPasamanos(string material)
        {
            if (string.IsNullOrWhiteSpace(material))
                return BadRequest("Material requerido");

            material = material.ToUpper();

            var query = _context.Productos
                .AsNoTracking()
                .Where(p =>
                    p.Activo &&
                    p.Descripcion.ToUpper().Contains("TUBO")
                );

            // 🔹 FILTRO POR MATERIAL (ESTADO CONCEPTUAL)
            if (material == "PESADO")
            {
                query = query.Where(p =>
                    p.Descripcion.ToUpper().Contains("PESADO")
                );
            }
            else if (material == "ACERO")
            {
                query = query.Where(p =>
                    p.Descripcion.ToUpper().Contains("ACERADO") ||
                    p.Descripcion.ToUpper().Contains("INOX")
                );
            }
            else
            {
                return BadRequest("Material inválido");
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
                    imagen = p.ImagenUrl,
                    conversionFactor = p.ConversionFactor
                })
                .ToListAsync();

            return Json(tubos);
        }

        [HttpGet]
        public async Task<IActionResult> GetAccesoriosPasamanos(string dimension)
        {
            if (string.IsNullOrWhiteSpace(dimension))
                return BadRequest("Dimensión requerida");

            var accesorios = await _context.Productos
                .AsNoTracking()
                .Where(p =>
                    p.Activo &&
                    p.Area == "ACCESORIOS" &&
                    p.Dimensiones == dimension &&                  // 👈 MISMA DIMENSIÓN
                    p.Descripcion.ToUpper().Contains("PESADOS")     // 👈 SOLO PESADOS
                )
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
        // CALCULAR COTIZACIÓN PASAMANOS
        // ================================
        [HttpPost]
        public async Task<IActionResult> CalcularPasamano(
            [FromBody] CotizacionPasamanoVm req)
        {
            if (req == null)
                return BadRequest("Datos inválidos");

            if (req.CantidadPiezas <= 0)
                return BadRequest("Cantidad inválida");

            var tubo = await _context.Productos
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.IdProducto == req.PasamanoProductoId);

            if (tubo == null)
                return NotFound("Pasamano no encontrado");

            decimal metrosTotales = 0;
            decimal subtotalTubo = 0;
            decimal precioUnitario = 0;

            // ================================
            // LÓGICA DE CÁLCULO (IGUAL A CORTINAS)
            // ================================
            if (req.TipoVenta == "VARILLA")
            {
                precioUnitario = tubo.PrecioVenta;
                subtotalTubo = req.CantidadPiezas * precioUnitario;
            }
            else if (req.TipoVenta == "METRO")
            {
                if (req.Metros <= 0)
                    return BadRequest("Metros inválidos");

                if (tubo.ConversionFactor <= 0)
                    return BadRequest("Factor de conversión inválido");

                // 🔥 REDONDEO COMERCIAL
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

            // ================================
            // ACCESORIOS (MÚLTIPLES)
            // ================================
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
                    var prod = productosAcc
                        .First(p => p.IdProducto == a.ProductoId);

                    var sub = prod.PrecioVenta * a.Cantidad;
                    subtotalAcc += sub;

                    accesoriosDetalle.Add(new
                    {
                        descripcion = prod.Descripcion,
                        cantidad = a.Cantidad,
                        subtotal = sub,
                        imagen = prod.ImagenUrl
                    });
                }
            }

            var total = subtotalTubo + subtotalAcc;

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
                total
            });
        }
    }
}
