namespace Negocio.Web.Models.Entities
{
    public class Reserva
    {
        public int ReservaId { get; set; }

        // Código público que ve el cliente
        public string CodigoReserva { get; set; } = string.Empty;

        // Datos del cliente
        public string NombreCliente { get; set; } = string.Empty;
        public string Telefono { get; set; } = string.Empty;
        // 📧 NUEVO CAMPO: Correo para notificaciones (Opcional)
        public string? Correo { get; set; }

        // JSON con detalle del pedido
        public string DetalleJson { get; set; } = string.Empty;

        // Totales
        public decimal Total { get; set; }
        public decimal Adelanto { get; set; }

        // 👉 Saldo automático (no guardado en BD)
        public decimal Saldo => Total - Adelanto;

        // Tipo de entrega
        public string TipoEntrega { get; set; } = "RECOJO";
        // RECOJO / DELIVERY

        public string? DireccionEntrega { get; set; }

        // Fechas
        public DateTime FechaRegistro { get; set; } = DateTime.Now;
        public DateTime? FechaSolicitada { get; set; }
        public DateTime? FechaConfirmada { get; set; }

        // Estado del pedido
        public string Estado { get; set; } = "PENDIENTE";

        /*
            PENDIENTE
            CONFIRMADO
            EN_PRODUCCION
            LISTO
            ENTREGADO
            CANCELADO
        */

        // 👉 Cancelación controlada
        public bool PuedeCancelar =>
            Estado == "PENDIENTE" || Estado == "CONFIRMADO";

        // Si la reserva es de Incógnito, se guardará como NULL automáticamente.
        // Si el usuario inició sesión, guardará el Id del cliente.
        public int? IdUsuario { get; set; }

        // Propiedad de navegación virtual (Opcional, muy útil para Entity Framework)
        public virtual Usuario? Usuario { get; set; }

        // Control interno
        public string? ObservacionesInternas { get; set; }
    }

}
