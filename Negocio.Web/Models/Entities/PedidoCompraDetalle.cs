using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Negocio.Web.Models.Entities
{
    public class PedidoCompraDetalle
    {
        [Key]
        public int IdPedidoCompraDetalle { get; set; }

        // 🔑 FK explícita
        public int IdPedidoCompra { get; set; }

        [ForeignKey(nameof(IdPedidoCompra))]
        public PedidoCompra PedidoCompra { get; set; } = null!;

        public int IdProducto { get; set; }

        public string Area { get; set; } = "";
        public string Producto { get; set; } = "";
        public string? Dimensiones { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal StockActual { get; set; }
    }
}
