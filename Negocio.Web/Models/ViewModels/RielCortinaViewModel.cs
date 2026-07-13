using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace Negocio.Web.Models.ViewModels
{
    public class RielCortinaViewModel
    {
        [Required]
        [Display(Name = "Metros")]
        public decimal Metros { get; set; }

        [Required]
        [Display(Name = "Tipo de cruce")]
        public string TipoCruce { get; set; } = "";

        public List<SelectListItem> TiposCruce { get; set; } = new()
        {
            new SelectListItem("Corre un lado", "UNO"),
            new SelectListItem("Corre ambos lados", "AMBOS")
        };

        // =========================
        // ACCESORIO EXTRA
        // =========================

        public bool AccesorioAparte { get; set; }

        public string? TipoUnera { get; set; }

        public List<SelectListItem> TiposUnera { get; set; } = new()
        {
            new SelectListItem("UÑERA - NORMAL", "NORMAL"),
            new SelectListItem("UÑERA - TECHO", "TECHO"),
            new SelectListItem("UÑERA - PARED", "PARED")
        };

        public decimal? CantidadUneraExtra { get; set; }

        // =========================================
        // ⚡ FECHA Y DÍA (AGREGADO PARA EL FILTRO)
        // =========================================

        [Required]
        public DateTime Fecha { get; set; }

        [Required]
        public string Dia { get; set; } = "";

        public List<SelectListItem> Dias { get; set; } = new()
        {
            new SelectListItem("LUNES", "LUNES"),
            new SelectListItem("MARTES", "MARTES"),
            new SelectListItem("MIÉRCOLES", "MIÉRCOLES"),
            new SelectListItem("JUEVES", "JUEVES"),
            new SelectListItem("VIERNES", "VIERNES"),
            new SelectListItem("SÁBADO", "SÁBADO"),
            new SelectListItem("DOMINGO", "DOMINGO")
        };

        // =========================
        // VENTA
        // =========================

        [Required]
        public decimal Precio { get; set; }

        public string Destino { get; set; } = "";

        public List<SelectListItem> Destinos { get; set; } = new()
        {
            new SelectListItem("EFECTIVO", "EFECTIVO"),
            new SelectListItem("YAPE", "YAPE")
        };
    }
}