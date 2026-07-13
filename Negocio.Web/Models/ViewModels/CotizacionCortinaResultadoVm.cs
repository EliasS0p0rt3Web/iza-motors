namespace Negocio.Web.Models.ViewModels
{
    // ================================
    // VIEWMODEL RESULTADO COTIZACIÓN
    // ================================
    public class CotizacionCortinaResultadoVm
    {
        public string TipoVenta { get; set; } = "";

        public string Tubo { get; set; } = "";

        public decimal PrecioUnitario { get; set; }

        public decimal? MetrosTotales { get; set; }

        public int? CantidadVarillas { get; set; }

        public decimal SubtotalTubo { get; set; }

        public List<CotizacionAccesorioResultadoVm> Accesorios { get; set; }
            = new();

        public decimal SubtotalAcc { get; set; }

        public decimal Total { get; set; }
    }

    public class CotizacionAccesorioResultadoVm
    {
        public string Descripcion { get; set; } = "";

        public int Cantidad { get; set; }

        public decimal Subtotal { get; set; }

        public string? Imagen { get; set; }
    }
}
