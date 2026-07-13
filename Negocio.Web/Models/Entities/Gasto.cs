using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Negocio.Web.Models.Entities
{
    [Table("Gastos")]
    public class Gasto
    {
        [Key]
        public int IdGasto { get; set; }

        [Required, StringLength(50)]
        public string Categoria { get; set; } = null!;

        [Required, StringLength(150)]
        public string Descripcion { get; set; } = null!;

        [Column(TypeName = "decimal(18,2)")]
        [Range(0.01, double.MaxValue)]
        public decimal Total { get; set; }

        [Required, StringLength(15)]
        public string Dia { get; set; } = null!;

        public DateTime FechaRegistro { get; set; }
    }
}
