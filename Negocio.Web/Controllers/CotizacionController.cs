using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Negocio.Web.Data;
using Negocio.Web.Models.ViewModels;

namespace Negocio.Web.Controllers
{
    public class CotizacionController : Controller
    {
        private readonly NegocioDbContext _context;

        public CotizacionController(NegocioDbContext context)
        {
            _context = context;
        }

        // ================================
        // PANTALLA BASE
        // ================================
        public IActionResult Index()
        {
            return View();
        }

        // ================================
        // COTIZACIÓN TUBO CORTINAS
        // ================================
        public IActionResult TuboCortinas()
        {
            return View(new CotizacionCortinaVm());
        }

        // ================================
        // OBTENER UN TUBO POR DIÁMETRO
        // ================================
        [HttpGet]
        public async Task<IActionResult> GetTuboCortina(string diametro)
        {
            if (string.IsNullOrWhiteSpace(diametro))
                return BadRequest("Diámetro requerido");

            var tubo = await _context.Productos.AsNoTracking()
                .FirstOrDefaultAsync(p =>
                    p.Area == "ALUMINIO" &&
                    p.Descripcion.Contains("TUBO") &&
                    p.Dimensiones != null &&
                    p.Dimensiones.Contains(diametro)
                );

            if (tubo == null)
                return NotFound("No existe tubo con ese diámetro");

            return Json(new
            {
                idProducto = tubo.IdProducto,
                descripcion = tubo.Descripcion,
                dimension = tubo.Dimensiones,
                precio = tubo.PrecioVenta,
                unidad = tubo.Unidad,
                imagen = tubo.ImagenUrl   // 👈 ESTA ES LA CLAVE
            });

        }

        // ================================
        // LISTAR TODOS LOS TUBOS DE CORTINA
        // ================================
        [HttpGet]
        public async Task<IActionResult> GetTubosCortina(string material)
        {
            if (string.IsNullOrWhiteSpace(material))
                return BadRequest("Material requerido");

            material = material.Trim().ToUpper();

            var query = _context.Productos
                .AsNoTracking()
                .Where(p =>
                    p.Activo &&
                    p.Descripcion.StartsWith("TUBO") &&
                    p.Dimensiones != null
                );

            // ================================
            // 🔹 FILTRO POR MATERIAL
            // ================================
            if (material == "ALUMINIO")
            {
                query = query.Where(p => p.Area == "ALUMINIO");
            }
            else if (material == "ACERO")
            {
                query = query.Where(p =>
                    p.Descripcion.ToUpper().Contains("ACERADO") ||
                    p.Descripcion.ToUpper().Contains("CROMADO")
                );
            }

            // ================================
            // 🔴 AQUI CONTROLAS LAS MEDIDAS QUE QUIERES QUE SALGAN
            // ================================
            var medidasPermitidas = new[]
            {
        "3/4",
        "1 PULGADA",
        "7/8",
        "30 MM",
        "32 MM"
        
    };

            query = query.Where(p =>
                medidasPermitidas.Contains(p.Dimensiones.Trim())
            );

            // ================================
            // 🔴 AQUI CONTROLAS EL TIPO DE TUBO POR DESCRIPCIÓN
            // ================================

            var descripcionPermitida = new[]
            {
        "TUBO ALUMINIO",
        "TUBO ACERADO",
        "TUBO ALUMINIO PESADO"
        // Puedes agregar:
        // "TUBO ALUMINIO PESADO",
        // "TUBO CUADRADO"
    };

            query = query.Where(p =>
                descripcionPermitida.Contains(p.Descripcion.ToUpper())
            );

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





        // ================================
        // ACCESORIOS PARA CORTINA
        // ================================
        [HttpGet]
        public async Task<IActionResult> GetAccesoriosCortina(
    string diametro,
    string instalacion,
    string uso)
        {
            var query = _context.Productos
                .AsNoTracking()
                .Where(p =>
                    p.Activo &&
                    p.Area == "ACCESORIOS" &&
                    p.Dimensiones.Contains(diametro) // 🔥 siempre según tubo
                );

            // =====================================================
            // TECHO + FÁCIL DE SACAR
            // =====================================================
            if (instalacion == "TECHO" && uso == "ABIERTO")
            {
                query = query.Where(p =>
                    p.Descripcion.Contains("PASANTE ACERADO") ||
                    p.Descripcion.Contains("PASANTES")
                )
                .Where(p => p.Dimensiones.Contains("TECHO"));
            }

            // =====================================================
            // TECHO + INSTALACIÓN FIJA
            // =====================================================
            else if (instalacion == "TECHO" && uso == "CERRADO")
            {
                query = query.Where(p =>
                    p.Descripcion.Contains("PASANTES") ||
                    p.Descripcion.Contains("TERMINALES") ||
                    p.Descripcion.Contains("TERMINALES PESADOS") ||
                    p.Descripcion.Contains("PASANTES PESADOS")
                );
            }

            // =====================================================
            // PARED + FÁCIL DE SACAR
            // =====================================================
            else if (instalacion == "PARED" && uso == "ABIERTO")
            {
                query = query.Where(p =>
                    p.Descripcion.Contains("PASANTES AVIERTOS") ||
                    p.Descripcion.Contains("BRIDA AVIERTA") ||
                    p.Descripcion.Contains("PASANTES")
                );
            }

            // =====================================================
            // PARED + INSTALACIÓN FIJA
            // =====================================================
            else if (instalacion == "PARED" && uso == "CERRADO")
            {
                query = query.Where(p =>
                    p.Descripcion.Contains("TERMINALES") ||
                    p.Descripcion.Contains("BRIDAS") ||
                    p.Descripcion.Contains("PASANTES") ||
                    p.Descripcion.Contains("TERMINALES PESADOS") ||
                    p.Descripcion.Contains("PASANTES PESADOS")
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
                    imagen = p.ImagenUrl
                })
                .ToListAsync();

            return Json(accesorios);
        }





        // ================================
        // CALCULAR COTIZACIÓN CORTINA
        // ================================
        [HttpPost]
        public async Task<IActionResult> CalcularCortina(
            [FromBody] CotizacionCortinaRequest req)
        {
            if (req == null)
                return BadRequest("Datos inválidos");

            if (req.CantidadPiezas <= 0)
                return BadRequest("Cantidad inválida");

            var tubo = await _context.Productos
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.IdProducto == req.TuboProductoId);

            if (tubo == null)
                return NotFound("Tubo no encontrado");

            decimal metrosTotales = 0;
            decimal subtotalTubo = 0;
            decimal precioUnitario = 0;

            // ================================
            // CÁLCULO DEL TUBO
            // ================================
            if (req.TipoVenta == "VARILLA")
            {
                precioUnitario = tubo.PrecioVenta;
                subtotalTubo = req.CantidadPiezas * precioUnitario;
            }
            else if (req.TipoVenta == "METRO")
            {
                if (req.LargoMetros <= 0)
                    return BadRequest("Largo inválido");

                if (tubo.ConversionFactor <= 0)
                    return BadRequest("Factor de conversión inválido");

                // 🔥 REDONDEO COMERCIAL
                precioUnitario = Math.Ceiling(
                    tubo.PrecioVenta / tubo.ConversionFactor
                );

                metrosTotales = req.LargoMetros * req.CantidadPiezas;
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
                var ids = req.Accesorios
                    .Select(a => a.ProductoId)
                    .ToList();

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

            // ================================
            // RESPUESTA
            // ================================
            return Json(new
            {
                tipoVenta = req.TipoVenta,
                tubo = tubo.Descripcion,
                dimension = tubo.Dimensiones,
                precioUnitario,
                metrosTotales = req.TipoVenta == "METRO"
                    ? metrosTotales
                    : (decimal?)null,
                cantidadVarillas = req.TipoVenta == "VARILLA"
                    ? req.CantidadPiezas
                    : (int?)null,
                subtotalTubo,
                accesorios = accesoriosDetalle,
                subtotalAcc,
                total
            });
        }


    }
}
