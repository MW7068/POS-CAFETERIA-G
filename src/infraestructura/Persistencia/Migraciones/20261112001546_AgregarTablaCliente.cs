using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PosCafeteria.Infraestructura.Persistencia.Migraciones;

/// <inheritdoc />
public partial class AgregarTablaCliente : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "ClienteId",
            table: "Ventas",
            type: "INTEGER",
            nullable: true);

        migrationBuilder.CreateTable(
            name: "Clientes",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                Nombre = table.Column<string>(type: "TEXT", nullable: false),
                Celular = table.Column<string>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Clientes", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Ventas_ClienteId",
            table: "Ventas",
            column: "ClienteId");

        migrationBuilder.AddForeignKey(
            name: "FK_Ventas_Clientes_ClienteId",
            table: "Ventas",
            column: "ClienteId",
            principalTable: "Clientes",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_Ventas_Clientes_ClienteId",
            table: "Ventas");

        migrationBuilder.DropTable(
            name: "Clientes");

        migrationBuilder.DropIndex(
            name: "IX_Ventas_ClienteId",
            table: "Ventas");

        migrationBuilder.DropColumn(
            name: "ClienteId",
            table: "Ventas");
    }
}
