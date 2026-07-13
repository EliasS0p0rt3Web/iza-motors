namespace Negocio.Web.Models.Dtos
{
    public class CrearReservaDto
    {
        public string NombreCliente { get; set; }
        public string Telefono { get; set; }

        // 📧 Agregamos el mensajero para el correo
        public string? Correo { get; set; }
        public string DetalleJson { get; set; }
        public decimal? Total { get; set; }
        public decimal? Adelanto { get; set; }
        public string TipoEntrega { get; set; }
        public string? DireccionEntrega { get; set; }
        public DateTime? FechaSolicitada { get; set; }

        // 👉 NUEVO: Recibe el ID si el usuario está logueado en la web
        public int? IdUsuario { get; set; }
    }
}