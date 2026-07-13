using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Negocio.Web.Models.Entities
{
    public class PeriodoSemanal
    {
        [Key]
        public int IdPeriodoSemanal { get; set; }

        [Required]
        public DateTime FechaInicio { get; set; }

        [Required]
        public DateTime FechaFin { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal EfectivoGenerado { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal YapeGenerado { get; set; }

        public int DiasTrabajados { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal SueldoCalculado { get; set; }

        // =====================================================
        // RETIROS DEL JEFE
        // =====================================================

        [Column(TypeName = "decimal(18,2)")]
        public decimal CajaSaldoPendiente { get; set; }

        [Required]
        [StringLength(30)]
        public string EstadoCajaJefe { get; set; } = "PENDIENTE";
        // PENDIENTE, PARCIAL, RETIRADO

        // =====================================================
        // SUELDO
        // =====================================================

        [Column(TypeName = "decimal(18,2)")]
        public decimal SueldoSaldoPendiente { get; set; }

        [Required]
        [StringLength(30)]
        public string EstadoMiSueldo { get; set; } = "PENDIENTE";
        // PENDIENTE, PARCIAL, PAGADO

        public DateTime? UltimaFechaModificacion { get; set; }

        [StringLength(250)]
        public string? Observaciones { get; set; }

        [StringLength(50)]
        public string? RegistroMetodoPago { get; set; }

        // =====================================================
        // HISTORIAL REAL DE MOVIMIENTOS
        // =====================================================

        public ICollection<MovimientoPeriodoSemanal> Movimientos { get; set; }
            = new List<MovimientoPeriodoSemanal>();

        // =====================================================
        // CONTROL DE CONCURRENCIA
        // =====================================================

        [Timestamp]
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }
}