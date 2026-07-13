using System.Security.Cryptography;
using System.Text;
using Negocio.Web.Models.Entities;

namespace Negocio.Web.Data
{
    public static class DbSeeder
    {
        public static void SeedUsuarios(NegocioDbContext context)
        {
            // ADMIN
            if (!context.Usuarios.Any(u => u.Username == "Elias"))
            {
                var admin = new Usuario
                {
                    Username = "Elias",
                    PasswordHash = HashPassword("S0p0rt3E"),
                    Rol = "ADMINISTRADOR",
                    Activo = true
                };

                context.Usuarios.Add(admin);
            }

            // JEFE
            if (!context.Usuarios.Any(u => u.Username == "JesusTorres"))
            {
                var jefe = new Usuario
                {
                    Username = "JesusTorres",
                    PasswordHash = HashPassword("jesus123456"),
                    Rol = "JEFE",
                    Activo = true
                };

                context.Usuarios.Add(jefe);
            }

            context.SaveChanges();
        }

        private static string HashPassword(string password)
        {
            using var sha = SHA256.Create();
            return Convert.ToBase64String(
                sha.ComputeHash(Encoding.UTF8.GetBytes(password))
            );
        }
    }
}
