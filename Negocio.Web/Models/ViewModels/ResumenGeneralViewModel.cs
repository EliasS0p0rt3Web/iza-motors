using Negocio.Web.Models.Entities;

namespace Negocio.Web.Models.ViewModels
{
    public class ResumenGeneralViewModel
    {
        public decimal CajaEfectivo { get; set; }

        public DateTime Desde { get; set; }
        public DateTime Hasta { get; set; }

        // ===== EFECTIVO =====
        public decimal EfectivoAluminio { get; set; }
        public decimal EfectivoAccesorios { get; set; }
        public decimal EfectivoARC { get; set; }

        // ===== YAPE =====
        public decimal YapeAluminio { get; set; }
        public decimal YapeAccesorios { get; set; }
        public decimal YapeARC { get; set; }

        // ===== TOTALES =====
        public decimal TotalEfectivo { get; set; }
        public decimal TotalYape { get; set; }
        public decimal TotalVentas { get; set; }

        // ===== GASTOS =====
        public List<Gasto> Gastos { get; set; } = new();
        public decimal TotalGastos { get; set; }

        // ===== RESULTADO =====
        public decimal ResultadoFinal { get; set; }
    }
}
