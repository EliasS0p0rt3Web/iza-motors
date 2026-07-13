using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Negocio.Web.Migrations
{
    public partial class nuevo : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ConfiguracionTienda",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EstaAbierto = table.Column<bool>(type: "bit", nullable: false),
                    MensajeEstado = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UltimaActualizacion = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfiguracionTienda", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Usuarios",
                columns: table => new
                {
                    IdUsuario = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Username = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Rol = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuarios", x => x.IdUsuario);
                });

            migrationBuilder.InsertData(
                table: "ConfiguracionTienda",
                columns: new[] { "Id", "EstaAbierto", "MensajeEstado", "UltimaActualizacion" },
                values: new object[] { 1, true, "Bienvenido, estamos atendiendo", new DateTime(2026, 6, 8, 0, 43, 50, 203, DateTimeKind.Local).AddTicks(2863) });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConfiguracionTienda");

            migrationBuilder.DropTable(
                name: "Usuarios");
        }
    }
}
