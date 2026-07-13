using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Negocio.Web.Models.Entities
{
    /// <summary>
    /// Registra cada movimiento financiero realizado sobre un periodo semanal.
    ///
    /// Ejemplos:
    /// - Pago de sueldo.
    /// - Retiro de caja.
    /// - Ajuste manual.
    ///
    /// Esta tabla funcionará como historial contable y evitará depender
    /// únicamente del campo Observaciones de PeriodoSemanal.
    /// </summary>
    public class MovimientoPeriodoSemanal
    {
        [Key]
        public int IdMovimientoPeriodoSemanal { get; set; }

        // =====================================================
        // RELACIÓN CON EL PERIODO SEMANAL
        // =====================================================

        [Required]
        public int IdPeriodoSemanal { get; set; }

        [ForeignKey(nameof(IdPeriodoSemanal))]
        public PeriodoSemanal PeriodoSemanal { get; set; } = null!;

        // =====================================================
        // INFORMACIÓN DEL MOVIMIENTO
        // =====================================================

        /// <summary>
        /// Valores permitidos inicialmente:
        /// PAGO_SUELDO
        /// RETIRO_CAJA
        /// AJUSTE_SUELDO
        /// AJUSTE_CAJA
        /// </summary>
        [Required]
        [StringLength(30)]
        public string TipoMovimiento { get; set; } = string.Empty;

        /// <summary>
        /// Importe exacto aplicado a este periodo.
        ///
        /// Por ejemplo, si un pago de S/ 300 se reparte entre tres periodos,
        /// cada periodo tendrá su propio movimiento con el monto realmente
        /// aplicado: S/ 100, S/ 150 y S/ 50.
        /// </summary>
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Monto { get; set; }

        /// <summary>
        /// Método utilizado para un pago de sueldo.
        ///
        /// Ejemplos:
        /// EFECTIVO
        /// YAPE
        /// TRANSFERENCIA
        ///
        /// Para retiros de caja puede quedar NULL.
        /// </summary>
        [StringLength(50)]
        public string? MetodoPago { get; set; }

        /// <summary>
        /// Detalle adicional proporcionado por el usuario.
        /// </summary>
        [StringLength(500)]
        public string? Observaciones { get; set; }

        /// <summary>
        /// Fecha y hora en que se registró el movimiento.
        ///
        /// El controlador asignará este valor al guardar.
        /// </summary>
        [Required]
        public DateTime FechaRegistro { get; set; }

        /// <summary>
        /// Usuario responsable de la operación.
        ///
        /// Por ahora puede quedar NULL si todavía no tienes
        /// autenticación o usuarios implementados.
        /// </summary>
        [StringLength(150)]
        public string? UsuarioRegistro { get; set; }
    }
}