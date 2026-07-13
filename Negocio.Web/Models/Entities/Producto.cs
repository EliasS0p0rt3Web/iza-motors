using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Negocio.Web.Models.Entities
{
    public class Producto
    {
        [Key]
        public int IdProducto { get; set; }

        public string? ImagenUrl { get; set; }


        public bool Activo { get; set; } = true;
        [Required]
        [StringLength(100)]
        public string Descripcion { get; set; } = null!;

        [Required]
        [StringLength(50)]
        public string Area { get; set; } = null!;

        [Required]
        [StringLength(20)]
        public string Unidad { get; set; } = null!;

        [StringLength(50)]
        public string? Dimensiones { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal PrecioCompra { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal PrecioVenta { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal StockActual { get; set; }

        [Column(TypeName = "decimal(18,6)")]
        public decimal ConversionFactor { get; set; } = 1;
    }
}
