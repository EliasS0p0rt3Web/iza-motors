using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Negocio.Web.Models.Entities
{
    public class PerfilCliente
    {
        [Key]
        public int IdPerfilCliente { get; set; }

        // 🔗 Llave foránea que apunta a la tabla Usuarios
        [Required]
        public int IdUsuario { get; set; }

        // Datos Personales / Comerciales
        [Required(ErrorMessage = "El nombre o razón social es obligatorio")]
        [StringLength(150)]
        public string NombreCompleto { get; set; } = string.Empty;

        [StringLength(15)]
        public string? Documento { get; set; } // DNI o RUC

        [Required(ErrorMessage = "El teléfono celular es obligatorio")]
        [StringLength(20)]
        public string Telefono { get; set; } = string.Empty;

        [StringLength(250)]
        public string? DireccionRecu { get; set; } // Dirección recurrente de envío

        [StringLength(50)]
        public string? TipoCliente { get; set; } = "ESTANDAR";
        // "ESTANDAR" | "MAESTRO_TECNICO" | "MAYORISTA"

        // 🔄 Propiedad de navegación inversa para Entity Framework
        [ForeignKey("IdUsuario")]
        public virtual Usuario Usuario { get; set; } = null!;


        public int ActualizacionesHoy { get; set; } = 0;
        public DateTime? UltimaActualizacion { get; set; }
    }
}