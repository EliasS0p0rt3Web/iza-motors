namespace Negocio.Web.Models.ViewModels
{
    public class CarritoItemVm
    {
        public int IdProducto { get; set; }

        public string Area { get; set; } = "";
        public string Descripcion { get; set; } = "";
        public string? Dimensiones { get; set; }
        public string? ImagenUrl { get; set; }

        public decimal PrecioUnitario { get; set; }
        public decimal Cantidad { get; set; }
        // 🚀 AGREGA ESTA PROPIEDAD:
        public string TipoVenta { get; set; } = "UNIDAD";

        public decimal Subtotal => PrecioUnitario * Cantidad;
    }
}