using System;
using System.ComponentModel.DataAnnotations;

namespace Negocio.Web.Models.Entities
{
    public class TrabajoMarketing
    {
        [Key]
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string? Titulo { get; set; }

        [StringLength(255)]
        public string? Descripcion { get; set; }

        [Required]
        public string? ImagenUrl { get; set; }

        [StringLength(50)]
        public string? Categoria { get; set; } // El filtro maestro: "Vidrieria", "Cortinas", "Pasamanos", etc.

        public bool Activo { get; set; } = true;

        public DateTime FechaCreacion { get; set; } = DateTime.Now;
    }
}