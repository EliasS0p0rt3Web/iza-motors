using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Negocio.Web.Models.Entities
{
    public class Ingreso
    {
        [Key]
        public int IdIngreso { get; set; }

        public int IdProducto { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal Cantidad { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? PrecioUnitario { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? PrecioPorMetro { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? PrecioPorVarilla { get; set; }

        // 🔥 ESTE ES EL CAMPO QUE FALTABA
        [StringLength(50)]
        public string? Dimensiones { get; set; }

        public DateTime FechaIngreso { get; set; }

        [ForeignKey(nameof(IdProducto))]
        public Producto? Producto { get; set; }
    }
}
