using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Negocio.Web.Models.ViewModels
{
    public class RegistrarProductoViewModel
    {
        [Required]
        public string Descripcion { get; set; } = null!;

        [Required]
        public string Area { get; set; } = null!;

        [Required]
        public string Unidad { get; set; } = null!;

        public string? Dimensiones { get; set; }

        [Required]
        public decimal PrecioCompra { get; set; }

        [Required]
        public decimal PrecioVenta { get; set; }

        [Required]
        public decimal StockInicial { get; set; }

        public decimal ConversionFactor { get; set; } = 1;

        // Combos
        public List<SelectListItem> Areas { get; set; } = new()
        {
            new("ALUMINIO", "ALUMINIO"),
            new("ACCESORIOS", "ACCESORIOS"),
            new("ARC", "ARC")
        };

        public List<SelectListItem> Unidades { get; set; } = new()
        {
            new("VARILLAS", "VARILLAS"),
            new("METROS", "METROS"),
            new("UNIDAD", "UNIDAD"),
            new("PAR", "PAR")
        };
    }
}
