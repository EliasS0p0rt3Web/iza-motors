namespace Negocio.Web.Models.ViewModels
{
    public class MasVendidoViewModel
    {
        public string Area { get; set; } = "";
        public string Producto { get; set; } = "";
        public decimal CantidadTotal { get; set; }
        public string Unidad { get; set; } = "";
    }
}