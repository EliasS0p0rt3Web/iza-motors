using System.ComponentModel.DataAnnotations;

namespace Negocio.Web.Models.Entities
{
    public class ConfiguracionTienda
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public bool EstaAbierto { get; set; }

        [StringLength(100)]
        public string? MensajeEstado { get; set; } // Por si quieres poner "Cerrados hasta mañana"

        public DateTime UltimaActualizacion { get; set; }
    }
}