using System;
using System.ComponentModel.DataAnnotations;

namespace Negocio.Web.Models.Entities
{
    public class SugerenciaSistema
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(1000)]
        public string Comentario { get; set; } = null!;

        [Required]
        [StringLength(50)]
        public string TipoFeedback { get; set; } = null!; // "Sugerencia", "Critica", "Felicitacion"

        [Required]
        public DateTime FechaRegistro { get; set; } = DateTime.Now;

        public bool Revisado { get; set; } = false;
    }
}