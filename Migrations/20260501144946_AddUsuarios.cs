using Microsoft.EntityFrameworkCore.Migrations;
#nullable disable

namespace Project_v1.Migrations
{
    // Migración que crea la tabla Usuarios en la base de datos
    // Generada automáticamente por Entity Framework Core
    public partial class AddUsuarios : Migration
    {
        // Define los cambios a aplicar en la base de datos (crear tabla)
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Crea la tabla Usuarios con todas sus columnas
            migrationBuilder.CreateTable(
                name: "Usuarios",
                columns: table => new
                {
                    // Clave primaria autoincremental
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),

                    // Nombre del usuario para iniciar sesión
                    Nombre = table.Column<string>(type: "TEXT", nullable: false),

                    // Contraseña del usuario
                    Contrasena = table.Column<string>(type: "TEXT", nullable: false),

                    // Rol del usuario: 0 = Administrador, 1 = Cajero
                    Rol = table.Column<int>(type: "INTEGER", nullable: false),

                    // Indica si el usuario está activo (eliminación lógica)
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    // Define Id como clave primaria de la tabla
                    table.PrimaryKey("PK_Usuarios", x => x.Id);
                });
        }

        // Define cómo revertir esta migración (eliminar la tabla)
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Elimina la tabla Usuarios si se revierte la migración
            migrationBuilder.DropTable(
                name: "Usuarios");
        }
    }
}