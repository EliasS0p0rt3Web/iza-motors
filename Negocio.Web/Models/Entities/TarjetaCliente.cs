using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Negocio.Web.Models.Entities
{
    public class TarjetaCliente
    {
        [Key]
        public int IdTarjeta { get; set; }

        [Required]
        public int IdUsuario { get; set; }

        [Required]
        [StringLength(100)]
        public string CardholderName { get; set; } = string.Empty; // Nombre del titular

        [Required]
        [StringLength(50)]
        public string CardBrand { get; set; } = string.Empty; // "visa", "mastercard", etc.

        [Required]
        [StringLength(4)]
        public string UltimosCuatro { get; set; } = string.Empty; // Para mostrar de forma segura en la UI

        [Required]
        [StringLength(150)]
        public string TokenReferencialMP { get; set; } = string.Empty; // Customer Token o Card Token de Mercado Pago

        public bool Activo { get; set; } = true;

        [ForeignKey("IdUsuario")]
        public virtual Usuario? Usuario { get; set; }
    }
}