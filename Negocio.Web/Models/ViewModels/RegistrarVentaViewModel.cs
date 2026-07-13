using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Negocio.Web.Models.ViewModels
{
    public class RegistrarVentaViewModel
    {

        public int? IdVenta { get; set; }


        // =========================
        // DATOS DE FECHA / DÍA
        // =========================
        [Required]
        public string Dia { get; set; } = "";

        [Required]
        public DateTime Fecha { get; set; } = DateTime.Today;

        // =========================
        // DATOS DEL PRODUCTO
        // =========================
        public string Area { get; set; } = "";

        public int? IdProducto { get; set; }

        public string? Descripcion { get; set; }

        public string? Dimensiones { get; set; }

        [Range(0.0001, double.MaxValue, ErrorMessage = "Cantidad inválida")]
        public decimal Cantidad { get; set; }

        // =========================
        // DATOS DE VENTA
        // =========================
        [Required]
        public string Unidad { get; set; } = "";

        [Required]
        public string Destino { get; set; } = "";

        [Range(0, double.MaxValue)]
        public decimal Precio { get; set; }

        public bool ActualizarStock { get; set; } = true;

        // =========================
        // COMBOS
        // =========================
        public List<SelectListItem> Dias { get; set; } = new()
        {
            new("LUNES","LUNES"),
            new("MARTES","MARTES"),
            new("MIÉRCOLES","MIÉRCOLES"),
            new("JUEVES","JUEVES"),
            new("VIERNES","VIERNES"),
            new("SÁBADO","SÁBADO"),
            new("DOMINGO","DOMINGO")
        };

        public List<SelectListItem> Areas { get; set; } = new();

        public List<SelectListItem> Unidades { get; set; } = new()
        {
            new("METROS", "METROS"),
            new("VARILLAS", "VARILLAS"),
            new("UNIDAD", "UNIDAD"),
            new("PAR", "PAR")
        };

        public List<SelectListItem> Destinos { get; set; } = new()
        {
            new("EFECTIVO","EFECTIVO"),
            new("YAPE","YAPE"),
            new("PLIN","PLIN")
        };
    }
}
