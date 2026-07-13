namespace Negocio.Web.Models.ViewModels
{
    public class RielCortinaResultadoViewModel
    {
        // =============================
        // CANTIDAD DESCONTADA
        // =============================

        public decimal RielDescontado { get; set; }
        public int UneraNormalDescontado { get; set; }
        public int CruceDescontado { get; set; }
        public int TerminalDescontado { get; set; }
        public int RuedaDescontado { get; set; }

        // =============================
        // STOCK ANTES DE LA VENTA
        // =============================

        public decimal StockRielAntes { get; set; }
        public decimal StockUneraAntes { get; set; }
        public decimal StockCruceAntes { get; set; }
        public decimal StockTerminalAntes { get; set; }
        public decimal StockRuedaAntes { get; set; }

        // =============================
        // STOCK DESPUÉS DE LA VENTA
        // =============================

        public decimal StockRiel { get; set; }
        public decimal StockUnera { get; set; }
        public decimal StockCruce { get; set; }
        public decimal StockTerminal { get; set; }
        public decimal StockRueda { get; set; }
    }
}