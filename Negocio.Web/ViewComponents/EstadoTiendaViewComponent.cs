using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Negocio.Web.Data;
using System.Threading.Tasks;

namespace Negocio.Web.ViewComponents
{
    public class EstadoTiendaViewComponent : ViewComponent
    {
        private readonly NegocioDbContext _context;

        public EstadoTiendaViewComponent(NegocioDbContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var config = await _context.ConfiguracionesTienda.FirstOrDefaultAsync(x => x.Id == 1);
            // Si por error no hay nada en la BD, devolvemos un objeto por defecto (abierto)
            return View(config ?? new Models.Entities.ConfiguracionTienda { EstaAbierto = true });
        }
    }
}