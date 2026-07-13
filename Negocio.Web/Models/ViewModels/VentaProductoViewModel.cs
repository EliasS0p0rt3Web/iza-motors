namespace Negocio.Web.Models.ViewModels
{
    public class VentaProductoViewModel
    {
        public int IdVenta { get; set; }

        public DateTime Fecha { get; set; }
        public string Area { get; set; } = "";
        public string Producto { get; set; } = "";
        public string? Dimensiones { get; set; }
        public decimal Cantidad { get; set; }
        public string Unidad { get; set; } = "";
        public decimal Precio { get; set; }
        public string Destino { get; set; } = "";
    }
}