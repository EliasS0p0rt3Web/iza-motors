using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Negocio.Web.Models.Entities
{
    public class DireccionCliente
    {
        [Key]
        public int IdDireccion { get; set; }

        [Required]
        public int IdUsuario { get; set; }

        [Required]
        [StringLength(50)]
        public string TipoUbicacion { get; set; } = "DOMICILIO"; // "DOMICILIO" | "OBRA" | "SHALOM"

        [Required(ErrorMessage = "La dirección o nombre de agencia es obligatoria.")]
        [StringLength(250)]
        public string DireccionTexto { get; set; } = string.Empty;

        [Required(ErrorMessage = "El distrito o ciudad de destino es obligatorio.")]
        [StringLength(100)]
        public string Distrito { get; set; } = string.Empty;

        [StringLength(250)]
        public string? Referencia { get; set; }

        // Campos para auditoría logistica o datos de recojo en Shalom
        [StringLength(150)]
        public string? DatosReceptorAgencia { get; set; } // DNI y Nombre de quien recoge en Shalom

        public bool Activo { get; set; } = true;

        [ForeignKey("IdUsuario")]
        public virtual Usuario? Usuario { get; set; }
    }
}