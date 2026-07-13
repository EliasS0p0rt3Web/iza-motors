namespace Negocio.Web.Models.ViewModels
{
    // ================================
    // VIEWMODEL PARA LA VISTA
    // ================================
    public class CotizacionCortinaVm
    {
        public string DiametroSeleccionado { get; set; } = "1"; // "1" | "7/8"

        public decimal LargoMetros { get; set; } = 1;

        public int CantidadPiezas { get; set; } = 1;

        public int? TuboProductoId { get; set; }

        public string TipoVenta { get; set; } = "METRO";
        // METRO | VARILLA
    }

    // ================================
    // REQUEST DESDE JS (COTIZACIÓN)
    // ================================
    public class CotizacionCortinaRequest
    {
        public int TuboProductoId { get; set; }

        public string TipoVenta { get; set; } = "METRO";
        // METRO | VARILLA

        public decimal LargoMetros { get; set; }

        public int CantidadPiezas { get; set; }

        // 🔥 CARRITO DE ACCESORIOS
        public List<AccesorioCortinaRequest>? Accesorios { get; set; }
    }

    // ================================
    // ITEM DEL CARRITO
    // ================================
    public class AccesorioCortinaRequest
    {
        public int ProductoId { get; set; }

        public int Cantidad { get; set; }
    }
}
