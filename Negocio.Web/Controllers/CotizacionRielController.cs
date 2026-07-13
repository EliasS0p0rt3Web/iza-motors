using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Negocio.Web.Data;
using Negocio.Web.Models.ViewModels;

namespace Negocio.Web.Controllers
{
    public class CotizacionRielController : Controller
    {
        private readonly NegocioDbContext _context;

        public CotizacionRielController(NegocioDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            // Retorna la vista con el Wizard de 4 pasos que diseñamos
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetRieles()
        {
            // Buscamos cualquier producto que contenga "RIEL" y "CORTINA" sin importar el orden
            var rieles = await _context.Productos
                .AsNoTracking()
                .Where(p => p.Activo && p.Descripcion.Contains("RIEL") && p.Descripcion.Contains("CORTINA"))
                .Select(p => new {
                    idProducto = p.IdProducto,
                    descripcion = p.Descripcion,
                    dimension = p.Dimensiones,
                    precio = p.PrecioVenta,
                    conversionFactor = p.ConversionFactor
                })
                .ToListAsync();

            return Json(rieles);
        }

        [HttpPost]
        public async Task<IActionResult> Calcular([FromBody] RielRequest req)
        {
            // Solo validamos que el producto exista en E&E Aluminios
            var riel = await _context.Productos.AnyAsync(p => p.IdProducto == req.IdProducto);
            if (!riel) return NotFound();

            // REGLA DE ORO: 11 soles por metro (Todo incluido)
            decimal subtotalRiel = req.Metros * 11.00m;

            // REGLA DE LOS 5 SOLES: Solo si es PARED
            decimal costoExtraPared = req.Instalacion == "PARED" ? 5.00m : 0;

            decimal total = subtotalRiel + costoExtraPared;

            return Json(new
            {
                subtotalRiel,
                costoExtraPared,
                total,
                detalle = $"Riel de Cortina - {req.Metros}m ({req.Instalacion})"
            });
        }
    }

    // Clase para recibir la data del JS
    public class RielRequest
    {
        public int IdProducto { get; set; }
        public decimal Metros { get; set; }
        public string Instalacion { get; set; } // "PARED" o "TECHO"
        public string Apertura { get; set; }    // "AMBOS" o "UN_LADO"
    }
}