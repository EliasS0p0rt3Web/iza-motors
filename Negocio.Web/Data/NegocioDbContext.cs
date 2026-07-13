using Microsoft.EntityFrameworkCore;
using Negocio.Web.Models;
using Negocio.Web.Models.Entities;
using Negocio.Web.Models.ViewModels;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace Negocio.Web.Data
{
    public class NegocioDbContext : DbContext
    {
        public NegocioDbContext(DbContextOptions<NegocioDbContext> options)
            : base(options)
        {
        }

        public DbSet<Producto> Productos { get; set; }
        public DbSet<Venta> Ventas { get; set; }
        public DbSet<Ingreso> Ingresos { get; set; }
        public DbSet<Gasto> Gastos { get; set; }

        public DbSet<Usuario> Usuarios { get; set; }

        // 🔥 NUEVA TABLA REGISTRADA: Perfiles de los clientes frecuentes
        public DbSet<PerfilCliente> PerfilesClientes { get; set; }
        public DbSet<PedidoCompra> PedidosCompra { get; set; }
        public DbSet<PedidoCompraDetalle> PedidoCompraDetalles { get; set; }


        public DbSet<Reserva> Reservas { get; set; }

        public DbSet<DescripcionImagen> DescripcionImagenes { get; set; }

        // ✅ AQUI AGREGAMOS LA NUEVA TABLA GLOBAL
        public DbSet<TrabajoMarketing> TrabajosMarketing { get; set; }


        // ✅ NUEVA TABLA REGISTRADA AQUÍ
        public DbSet<PeriodoSemanal> PeriodosSemanales { get; set; }
        public DbSet<MovimientoPeriodoSemanal> MovimientosPeriodoSemanal { get; set; }
        public DbSet<ConfiguracionTienda> ConfiguracionesTienda { get; set; }

        public DbSet<SugerenciaSistema> SugerenciasSistema { get; set; }

        public DbSet<DireccionCliente> DireccionesClientes { get; set; }
        public DbSet<TarjetaCliente> TarjetasClientes { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Nombres de tablas exactos (opcional pero recomendado)
            modelBuilder.Entity<Producto>().ToTable("Producto");
            modelBuilder.Entity<Venta>().ToTable("Venta");
            modelBuilder.Entity<Ingreso>().ToTable("Ingreso");
            modelBuilder.Entity<Gasto>().ToTable("Gasto");

            // 🔥 SIGUIENDO TU ESTÁNDAR SINGULAR: Nombre físico de la tabla en BD
            modelBuilder.Entity<PerfilCliente>().ToTable("PerfilCliente");

            // Configuración explícita de la relación 1 a 1 entre Usuario y PerfilCliente
            modelBuilder.Entity<PerfilCliente>()
                .HasOne(p => p.Usuario)
                .WithMany() // Un usuario puede tener los datos de su perfil amarrados
                .HasForeignKey(p => p.IdUsuario)
                .OnDelete(DeleteBehavior.Cascade); // Si se elimina el usuario, se elimina su perfil automáticamente

            // ✅ MANTENEMOS TU ESTÁNDAR
            modelBuilder.Entity<TrabajoMarketing>().ToTable("TrabajoMarketing");

            // ✅ AGREGA ESTA LÍNEA:
            modelBuilder.Entity<ConfiguracionTienda>().ToTable("ConfiguracionTienda");

            // ✅ REGISTRO EXPLICITAMENTE LA NUEVA TABLA CON TU ESTÁNDAR
            modelBuilder.Entity<PeriodoSemanal>().ToTable("PeriodoSemanal");

            modelBuilder.Entity<MovimientoPeriodoSemanal>()
    .ToTable("MovimientoPeriodoSemanal");

            modelBuilder.Entity<MovimientoPeriodoSemanal>()
                .HasOne(m => m.PeriodoSemanal)
                .WithMany(p => p.Movimientos)
                .HasForeignKey(m => m.IdPeriodoSemanal)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PeriodoSemanal>()
                .Property(p => p.RowVersion)
                .IsRowVersion();

            modelBuilder.Entity<PeriodoSemanal>()
                .HasCheckConstraint(
                    "CK_PeriodoSemanal_SaldosNoNegativos",
                    "[CajaSaldoPendiente] >= 0 AND [SueldoSaldoPendiente] >= 0 AND [EfectivoGenerado] >= 0 AND [YapeGenerado] >= 0 AND [SueldoCalculado] >= 0"
                );

            modelBuilder.Entity<MovimientoPeriodoSemanal>()
                .HasCheckConstraint(
                    "CK_MovimientoPeriodoSemanal_MontoPositivo",
                    "[Monto] > 0"
                );

            modelBuilder.Entity<MovimientoPeriodoSemanal>()
                .HasIndex(m => new { m.IdPeriodoSemanal, m.FechaRegistro });

            // ✅ REGISTRAMOS LA TABLA DE OPINIONES SIGUIENDO TU ESTÁNDAR SINGULAR
            modelBuilder.Entity<SugerenciaSistema>().ToTable("SugerenciaSistema");

            // (Opcional) Si quieres asegurar que el Id siempre empiece en 1 y sea fijo:
            modelBuilder.Entity<ConfiguracionTienda>().HasData(
                new ConfiguracionTienda { Id = 1, EstaAbierto = true, MensajeEstado = "Bienvenido, estamos atendiendo", UltimaActualizacion = DateTime.Now }
            );
            modelBuilder.Entity<DireccionCliente>().ToTable("DireccionCliente");
            modelBuilder.Entity<TarjetaCliente>().ToTable("TarjetaCliente");
        }
    }
}
