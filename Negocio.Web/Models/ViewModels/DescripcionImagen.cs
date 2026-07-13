using System.ComponentModel.DataAnnotations;

namespace Negocio.Web.Models.ViewModels
{
    public class DescripcionImagen
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Area { get; set; } = null!; // Ejemplo: ALUMINIO, ACCESORIO

        [Required]
        [StringLength(100)]
        public string Descripcion { get; set; } = null!; // Ejemplo: TERMINALES, TUBO 1 PULGADA

        [Required]
        public string ImagenUrl { get; set; } = null!; // Ruta de la imagen general

        public DateTime FechaActualizacion { get; set; } = DateTime.Now;
    }
}