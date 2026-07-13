using Microsoft.AspNetCore.Mvc.Rendering;

namespace Negocio.Web.Models.ViewModels
{
    public class VentaIndexViewModel
    {
        // =========================
        // FILTROS
        // =========================
        public DateTime? Desde { get; set; }
        public DateTime? Hasta { get; set; }
        public string Area { get; set; } = "TODAS";
        public string Destino { get; set; } = "TODOS";

        // =========================
        // COMBOS
        // =========================
        public List<SelectListItem> Areas { get; set; } = new();
        public List<SelectListItem> Destinos { get; set; } = new();

        // =========================
        // RESULTADO
        // =========================
        public List<VentaProductoViewModel> Ventas { get; set; } = new();

        // =========================
        // TOTALES
        // =========================
        public decimal TotalGeneral { get; set; }
    }
}
