using Microsoft.AspNetCore.Mvc;
using Negocio.Web.Data; // IMPORTANTE: Para acceder a tu DB
using Negocio.Web.Models.Entities;
using System.Linq;

namespace Negocio.Web.Controllers
{
    public class HomePublicController : Controller
    {
        private readonly NegocioDbContext _context;

        public HomePublicController(NegocioDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            // Aquí jalamos las fotos de tu nueva tabla global
            var trabajos = _context.TrabajosMarketing
                                   .Where(t => t.Activo) // Solo las que están en "Mostrar"
                                   .OrderByDescending(t => t.FechaCreacion)
                                   .ToList();

            return View(trabajos); // Le pasamos la lista a la vista Index.cshtml
        }

        // 🟢 NUEVO: Este método filtrará las fotos según la categoría que elijas
        public IActionResult Galeria(string categoria)
        {
            // Si no eligen categoría, por defecto mostramos todo
            var query = _context.TrabajosMarketing.Where(t => t.Activo);

            if (!string.IsNullOrEmpty(categoria))
            {
                query = query.Where(t => t.Categoria == categoria);
            }

            var lista = query.OrderByDescending(t => t.FechaCreacion).ToList();

            // Le pasamos el nombre al ViewData para usarlo en el título de la vista
            ViewData["CategoriaNombre"] = string.IsNullOrEmpty(categoria) ? "Todos los Trabajos" : categoria;

            return View(lista);
        }


        [HttpPost]
        [ValidateAntiForgeryToken] // Seguridad para evitar ataques CSRF
        public async Task<IActionResult> GuardarSugerencia([FromBody] SugerenciaRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Comentario))
            {
                return BadRequest(new { success = false, message = "Por favor, escribe un comentario válido." });
            }

            try
            {
                var nuevaSugerencia = new SugerenciaSistema
                {
                    Comentario = request.Comentario.Trim(),
                    TipoFeedback = request.TipoFeedback ?? "Sugerencia",
                    FechaRegistro = DateTime.Now,
                    Revisado = false
                };

                _context.SugerenciasSistema.Add(nuevaSugerencia);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "¡Excelente! Tu opinión ha sido registrada para mejorar nuestro sistema." });
            }
            catch (Exception)
            {
                return StatusCode(500, new { success = false, message = "Hubo un inconveniente en el servidor al procesar la sugerencia." });
            }
        }

        // Estructura ligera para recibir el JSON desacoplado de la base de datos
        public class SugerenciaRequest
        {
            public string Comentario { get; set; } = null!;
            public string TipoFeedback { get; set; } = null!;
        }
    }
}