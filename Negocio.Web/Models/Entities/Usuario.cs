using System.ComponentModel.DataAnnotations;

namespace Negocio.Web.Models.Entities
{
    public class Usuario
    {
        [Key]
        public int IdUsuario { get; set; }

        [Required]
        public string Username { get; set; } = null!;

        [Required]
        public string PasswordHash { get; set; } = null!;

        [Required]
        public string Rol { get; set; } = null!;
        // "ADMINISTRADOR" | "JEFE" | "CLIENTE"



        public bool Activo { get; set; } = true;
    }
}
