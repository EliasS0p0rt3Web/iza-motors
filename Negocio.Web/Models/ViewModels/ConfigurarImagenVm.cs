using Microsoft.AspNetCore.Mvc.Rendering;

namespace Negocio.Web.Models.ViewModels
{
    public class ConfigurarImagenVm
    {
        public string Area { get; set; } = null!;
        public string Descripcion { get; set; } = null!;
        public IFormFile? Archivo { get; set; } // El archivo físico

        public List<SelectListItem>? Areas { get; set; }
    }
}