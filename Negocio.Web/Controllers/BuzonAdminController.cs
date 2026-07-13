using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Negocio.Web.Data;
using System.Linq;
using System.Threading.Tasks;

namespace Negocio.Web.Controllers
{
    public class BuzonAdminController : Controller
    {
        private readonly NegocioDbContext _context;

        public BuzonAdminController(NegocioDbContext context)
        {
            _context = context;
        }

        // Listado Principal de comentarios recibidos
        public async Task<IActionResult> Index()
        {
            // Ordenamos para que las sugerencias más recientes y las no revisadas salgan primero
            var listado = await _context.SugerenciasSistema
                .OrderBy(x => x.Revisado)
                .ThenByDescending(x => x.FechaRegistro)
                .ToListAsync();

            return View(listado);
        }

        // Método asíncrono para marcar una opinión como leída o revisada desde la tabla
        [HttpPost]
        public async Task<IActionResult> MarcarComoRevisado(int id)
        {
            var sugerencia = await _context.SugerenciasSistema.FindAsync(id);
            if (sugerencia == null)
            {
                return Json(new { success = false, message = "La sugerencia no existe." });
            }

            sugerencia.Revisado = true;
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Comentario marcado como revisado con éxito." });
        }
    }
}