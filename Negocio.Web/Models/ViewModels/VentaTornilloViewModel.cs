using Microsoft.AspNetCore.Mvc.Rendering;
using Negocio.Web.Models.Entities;

namespace Negocio.Web.Models.ViewModels
{
    public class VentaTornilloViewModel
    {
        public int IdProductoTornillo { get; set; }
        public int IdProductoTarubo { get; set; }

        public string TipoVenta { get; set; } = "";

        public int Cantidad { get; set; }

        public string Destino { get; set; } = "EFECTIVO";

        public decimal PrecioDocena { get; set; } = 2;

        public decimal Total { get; set; }

        public List<Producto> Tornillos { get; set; } = new();

        public List<Producto> Tarubos { get; set; } = new();


        // ⚡ AGREGA ESTO AL FINAL DE TU VENTA TORNILLO VIEWMODEL:
        public DateTime Fecha { get; set; }
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
    }
}