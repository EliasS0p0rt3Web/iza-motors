using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Negocio.Web.Models.ViewModels
{
    // =========================================================
    // MODELO PRINCIPAL DE LA PANTALLA
    // =========================================================
    public class PeriodoSemanalIndexViewModel
    {
        public List<PeriodoSemanalFilaViewModel> Periodos { get; set; }
            = new List<PeriodoSemanalFilaViewModel>();

        public List<PeriodoPendienteViewModel> PeriodosPendientes { get; set; }
            = new List<PeriodoPendienteViewModel>();

        public decimal SueldoAcumuladoTotal { get; set; }

        public decimal CajaAcumuladaTotal { get; set; }
    }

    // =========================================================
    // CADA FILA REAL DEL HISTORIAL
    // =========================================================
    public class PeriodoSemanalFilaViewModel
    {
        public int IdPeriodoSemanal { get; set; }

        public DateTime FechaInicio { get; set; }

        public DateTime FechaFin { get; set; }

        public decimal EfectivoGenerado { get; set; }

        public decimal YapeGenerado { get; set; }

        public int DiasTrabajados { get; set; }

        public decimal SueldoCalculado { get; set; }

        public decimal CajaSaldoPendiente { get; set; }

        public string EstadoCajaJefe { get; set; } = string.Empty;

        public decimal SueldoSaldoPendiente { get; set; }

        public string EstadoMiSueldo { get; set; } = string.Empty;

        public DateTime? UltimaFechaModificacion { get; set; }

        public string? Observaciones { get; set; }

        public string? RegistroMetodoPago { get; set; }

        public List<MovimientoPeriodoFilaViewModel> Movimientos { get; set; }
            = new List<MovimientoPeriodoFilaViewModel>();
    }

    // =========================================================
    // PERIODOS DISPONIBLES PARA CERRAR
    // =========================================================
    public class PeriodoPendienteViewModel
    {
        public DateTime FechaInicio { get; set; }

        public DateTime FechaFin { get; set; }

        public string Valor =>
            $"{FechaInicio:dd/MM/yyyy}|{FechaFin:dd/MM/yyyy}";

        public string Texto =>
            $"Del {FechaInicio:dd/MM/yyyy} al {FechaFin:dd/MM/yyyy}";
    }

    // =========================================================
    // MOVIMIENTO MOSTRADO EN EL HISTORIAL
    // =========================================================
    public class MovimientoPeriodoFilaViewModel
    {
        public int IdMovimientoPeriodoSemanal { get; set; }

        public string TipoMovimiento { get; set; } = string.Empty;

        public decimal Monto { get; set; }

        public string? MetodoPago { get; set; }

        public string? Observaciones { get; set; }

        public DateTime FechaRegistro { get; set; }

        public string? UsuarioRegistro { get; set; }
    }

    // =========================================================
    // REQUEST PARA CERRAR UN PERIODO NORMAL
    // =========================================================
    public class CerrarPeriodoRequest
    {
        [Required(ErrorMessage = "La fecha de inicio es obligatoria.")]
        public string FechaInicio { get; set; } = string.Empty;

        [Required(ErrorMessage = "La fecha final es obligatoria.")]
        public string FechaFin { get; set; } = string.Empty;
    }

    // =========================================================
    // REQUEST PARA CIERRE MANUAL O ESPECIAL
    // =========================================================
    public class CerrarPeriodoManualRequest
    {
        [Required(ErrorMessage = "La fecha de inicio es obligatoria.")]
        public string FechaInicio { get; set; } = string.Empty;

        [Required(ErrorMessage = "La fecha final es obligatoria.")]
        public string FechaFin { get; set; } = string.Empty;
    }

    // =========================================================
    // REQUEST PARA PAGO LIBRE DE SUELDO
    // =========================================================
    public class RegistrarPagoLibreRequest
    {
        [Range(
            typeof(decimal),
            "0.01",
            "9999999999999999.99",
            ErrorMessage = "El monto debe ser mayor a cero."
        )]
        public decimal Monto { get; set; }

        [Required(ErrorMessage = "Debes seleccionar un método de pago.")]
        [StringLength(50)]
        public string MetodoPago { get; set; } = string.Empty;

        [StringLength(
            500,
            ErrorMessage = "La observación no puede superar los 500 caracteres."
        )]
        public string? Observaciones { get; set; }
    }

    // =========================================================
    // REQUEST PARA RETIRO LIBRE DE CAJA
    // =========================================================
    public class RegistrarRetiroLibreRequest
    {
        [Range(
            typeof(decimal),
            "0.01",
            "9999999999999999.99",
            ErrorMessage = "El monto debe ser mayor a cero."
        )]
        public decimal Monto { get; set; }

        [StringLength(
            500,
            ErrorMessage = "La observación no puede superar los 500 caracteres."
        )]
        public string? Observaciones { get; set; }
    }

    // =========================================================
    // REQUEST PARA PAGAR TODO EL SUELDO DE UN PERIODO
    // =========================================================
    public class RegistrarPagoCompletoRequest
    {
        [Range(1, int.MaxValue, ErrorMessage = "El periodo no es válido.")]
        public int IdPeriodoSemanal { get; set; }

        [Required(ErrorMessage = "Debes seleccionar un método de pago.")]
        [StringLength(50)]
        public string MetodoPago { get; set; } = "EFECTIVO";

        [StringLength(
            500,
            ErrorMessage = "La observación no puede superar los 500 caracteres."
        )]
        public string? Observaciones { get; set; }
    }

    // =========================================================
    // REQUEST PARA RETIRAR TODA LA CAJA DE UN PERIODO
    // =========================================================
    public class RegistrarRetiroCompletoRequest
    {
        [Range(1, int.MaxValue, ErrorMessage = "El periodo no es válido.")]
        public int IdPeriodoSemanal { get; set; }

        [StringLength(
            500,
            ErrorMessage = "La observación no puede superar los 500 caracteres."
        )]
        public string? Observaciones { get; set; }
    }

    // =========================================================
    // RESPUESTA GENERAL PARA AJAX / FETCH
    // =========================================================
    public class OperacionPeriodoResponse
    {
        public bool Success { get; set; }

        public string Message { get; set; } = string.Empty;

        public object? Data { get; set; }

        public static OperacionPeriodoResponse Exito(
            string message,
            object? data = null)
        {
            return new OperacionPeriodoResponse
            {
                Success = true,
                Message = message,
                Data = data
            };
        }

        public static OperacionPeriodoResponse Error(string message)
        {
            return new OperacionPeriodoResponse
            {
                Success = false,
                Message = message
            };
        }
    }
}

