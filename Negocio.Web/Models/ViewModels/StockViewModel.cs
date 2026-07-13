namespace Negocio.Web.Models.ViewModels
{
    public class StockViewModel
    {
        public int IdProducto { get; set; }   // 👈 CLAVE
        public string Area { get; set; } = "";
        public string Producto { get; set; } = "";
        public string? Dimensiones { get; set; }
        public string Unidad { get; set; } = "";
        public decimal StockActual { get; set; }
    }
}
