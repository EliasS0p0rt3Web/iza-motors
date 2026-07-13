namespace Negocio.Web.Models.ViewModels
{
    public class ReporteVentasProductoViewModel
    {
        public int? ProductoId { get; set; }
        public string? NombreProducto { get; set; }

        public DateTime? Desde { get; set; }
        public DateTime? Hasta { get; set; }

        public decimal TotalCantidad { get; set; }
        public decimal TotalImporte { get; set; }

        public string? Unidad { get; set; }
    }
}