namespace Negocio.Web.Models.ViewModels
{
    public class CotizacionPasamanoVm
    {
        public int PasamanoProductoId { get; set; }

        // METRO / VARILLA
        public string TipoVenta { get; set; } = "";

        public decimal Metros { get; set; }
        public int CantidadPiezas { get; set; }

        public List<AccesorioVm> Accesorios { get; set; } = new();
    }

    public class AccesorioVm
    {
        public int ProductoId { get; set; }
        public int Cantidad { get; set; }
    }
}
