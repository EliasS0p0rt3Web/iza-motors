using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Negocio.Web.Models.Entities
{
    public class Venta
    {
        [Key]
        public int IdVenta { get; set; }

       
        public string Dia { get; set; } = null!;

        [Required, StringLength(50)]
        public string Area { get; set; } = null!;



        [Column(TypeName = "decimal(18,4)")]
        public decimal Cantidad { get; set; }

        [Required, StringLength(20)]
        public string Unidad { get; set; } = null!;

        [Required, StringLength(100)]
        public string Descripcion { get; set; } = null!;

        [StringLength(50)]
        public string? Dimensiones { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Precio { get; set; } // Total de la venta

        [Required, StringLength(50)]
        public string Destino { get; set; } = null!;

        public DateTime FechaRegistro { get; set; }

        public int? IdProducto { get; set; }

        [ForeignKey(nameof(IdProducto))]
        public Producto? Producto { get; set; }
    }
}
