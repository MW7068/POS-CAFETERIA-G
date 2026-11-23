using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PosCafeteria.Infraestructura.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class ModeloCompletoPbi03 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DetallesVenta_VentaId_ProductoId",
                table: "DetallesVenta");

            migrationBuilder.AddColumn<int>(
                name: "CajaId",
                table: "Ventas",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ClienteId",
                table: "Ventas",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Descuento",
                table: "Ventas",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "Estado",
                table: "Ventas",
                type: "INTEGER",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<string>(
                name: "IdentificadorOperacion",
                table: "Ventas",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Modalidad",
                table: "Ventas",
                type: "INTEGER",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "NumeroMesa",
                table: "Ventas",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UsuarioId",
                table: "Ventas",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "Activo",
                table: "Productos",
                type: "INTEGER",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<int>(
                name: "CategoriaId",
                table: "Productos",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StockMinimo",
                table: "Productos",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "NombreProducto",
                table: "DetallesVenta",
                type: "TEXT",
                nullable: false,
                defaultValue: "");



            // Recuperar el nombre disponible del producto
            // para los detalles de ventas anteriores.
                migrationBuilder.Sql("""
                UPDATE "DetallesVenta"
                SET "NombreProducto" = COALESCE(
                    (
                        SELECT "Nombre"
                        FROM "Productos"
                        WHERE "Productos"."Id" =
                            "DetallesVenta"."ProductoId"
                    ),
                    'Producto histórico sin nombre'
                )
                WHERE "NombreProducto" = '';
                """);

            migrationBuilder.AddColumn<string>(
                name: "Observacion",
                table: "DetallesVenta",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Categorias",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nombre = table.Column<string>(type: "TEXT", nullable: false),
                    Activa = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categorias", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Clientes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nombre = table.Column<string>(type: "TEXT", nullable: false),
                    TipoDocumento = table.Column<int>(type: "INTEGER", nullable: false),
                    NumeroDocumento = table.Column<string>(type: "TEXT", nullable: false),
                    Telefono = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clientes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Comprobantes",
                columns: table => new
                {
                    Idcomprobante = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    VentaId = table.Column<int>(type: "INTEGER", nullable: false),
                    Serie = table.Column<string>(type: "TEXT", nullable: false),
                    Numero = table.Column<int>(type: "INTEGER", nullable: false),
                    ClienteNombre = table.Column<string>(type: "TEXT", nullable: false),
                    Total = table.Column<decimal>(type: "TEXT", nullable: false),
                    FechaEmisionUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Comprobantes", x => x.Idcomprobante);
                    table.ForeignKey(
                        name: "FK_Comprobantes_Ventas_VentaId",
                        column: x => x.VentaId,
                        principalTable: "Ventas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Negocios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    NombreNegocio = table.Column<string>(type: "TEXT", nullable: false),
                    Ruc = table.Column<string>(type: "TEXT", nullable: true),
                    TasaImpuesto = table.Column<decimal>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Negocios", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Usuarios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    NombreUsuario = table.Column<string>(type: "TEXT", nullable: false),
                    Password = table.Column<string>(type: "TEXT", nullable: false),
                    Rol = table.Column<int>(type: "INTEGER", nullable: false),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuarios", x => x.Id);
                });



                // Usuario técnico para ventas anteriores.
                // Solo se crea si existen ventas históricas.
                // Permanece desactivado y sin contraseña utilizable.

                migrationBuilder.Sql("""
                    INSERT INTO "Usuarios"
                        ("NombreUsuario", "Password", "Rol", "Activo")
                    SELECT
                        '__ventas_historicas__',
                        lower(hex(randomblob(32))),
                        2,
                        0
                    WHERE EXISTS (
                        SELECT 1
                        FROM "Ventas"
                        WHERE "UsuarioId" = 0
                    );

                    UPDATE "Ventas"
                    SET "UsuarioId" = (
                        SELECT "Id"
                        FROM "Usuarios"
                        WHERE "NombreUsuario" = '__ventas_historicas__'
                    )
                    WHERE "UsuarioId" = 0;
                    """);

            migrationBuilder.CreateTable(
                name: "Cajas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UsuarioId = table.Column<int>(type: "INTEGER", nullable: false),
                    FechaAperturaUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FechaCierreUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    MontoInicial = table.Column<decimal>(type: "TEXT", nullable: false),
                    MontoContado = table.Column<decimal>(type: "TEXT", nullable: true),
                    TotalIngresosEfectivo = table.Column<decimal>(type: "TEXT", nullable: true),
                    TotalEgresosEfectivo = table.Column<decimal>(type: "TEXT", nullable: true),
                    TotalEsperado = table.Column<decimal>(type: "TEXT", nullable: true),
                    Diferencia = table.Column<decimal>(type: "TEXT", nullable: true),
                    Estado = table.Column<int>(type: "INTEGER", nullable: false),
                    Observacion = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cajas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Cajas_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MovimientosStock",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProductoId = table.Column<int>(type: "INTEGER", nullable: false),
                    UsuarioId = table.Column<int>(type: "INTEGER", nullable: false),
                    VentaId = table.Column<int>(type: "INTEGER", nullable: true),
                    Tipo = table.Column<int>(type: "INTEGER", nullable: false),
                    Cantidad = table.Column<int>(type: "INTEGER", nullable: false),
                    Motivo = table.Column<string>(type: "TEXT", nullable: false),
                    FechaUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MovimientosStock", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MovimientosStock_Productos_ProductoId",
                        column: x => x.ProductoId,
                        principalTable: "Productos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MovimientosStock_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MovimientosStock_Ventas_VentaId",
                        column: x => x.VentaId,
                        principalTable: "Ventas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CajaMovimientos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CajaId = table.Column<int>(type: "INTEGER", nullable: false),
                    UsuarioId = table.Column<int>(type: "INTEGER", nullable: false),
                    VentaId = table.Column<int>(type: "INTEGER", nullable: true),
                    Tipo = table.Column<int>(type: "INTEGER", nullable: false),
                    Concepto = table.Column<string>(type: "TEXT", nullable: false),
                    MetodoPago = table.Column<int>(type: "INTEGER", nullable: false),
                    Monto = table.Column<decimal>(type: "TEXT", nullable: false),
                    Referencia = table.Column<string>(type: "TEXT", nullable: true),
                    FechaUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Observacion = table.Column<string>(type: "TEXT", nullable: true),
                    Origen = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CajaMovimientos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CajaMovimientos_Cajas_CajaId",
                        column: x => x.CajaId,
                        principalTable: "Cajas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CajaMovimientos_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CajaMovimientos_Ventas_VentaId",
                        column: x => x.VentaId,
                        principalTable: "Ventas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Pagos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    VentaId = table.Column<int>(type: "INTEGER", nullable: false),
                    CajaId = table.Column<int>(type: "INTEGER", nullable: false),
                    UsuarioId = table.Column<int>(type: "INTEGER", nullable: false),
                    Metodo = table.Column<int>(type: "INTEGER", nullable: false),
                    MontoConsumo = table.Column<decimal>(type: "TEXT", nullable: false),
                    MontoPropina = table.Column<decimal>(type: "TEXT", nullable: false),
                    MontoTotal = table.Column<decimal>(type: "TEXT", nullable: false),
                    MontoRecibido = table.Column<decimal>(type: "TEXT", nullable: false),
                    ConfirmacionManualDigital = table.Column<bool>(type: "INTEGER", nullable: false),
                    ReferenciaOperacionDigital = table.Column<string>(type: "TEXT", nullable: true),
                    FechaPagoUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pagos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Pagos_Cajas_CajaId",
                        column: x => x.CajaId,
                        principalTable: "Cajas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Pagos_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Pagos_Ventas_VentaId",
                        column: x => x.VentaId,
                        principalTable: "Ventas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Ventas_CajaId",
                table: "Ventas",
                column: "CajaId");

            migrationBuilder.CreateIndex(
                name: "IX_Ventas_ClienteId",
                table: "Ventas",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_Ventas_IdentificadorOperacion",
                table: "Ventas",
                column: "IdentificadorOperacion",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Ventas_UsuarioId",
                table: "Ventas",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Productos_CategoriaId",
                table: "Productos",
                column: "CategoriaId");

            migrationBuilder.CreateIndex(
                name: "IX_DetallesVenta_VentaId_ProductoId",
                table: "DetallesVenta",
                columns: new[] { "VentaId", "ProductoId" });

            migrationBuilder.CreateIndex(
                name: "IX_CajaMovimientos_CajaId",
                table: "CajaMovimientos",
                column: "CajaId");

            migrationBuilder.CreateIndex(
                name: "IX_CajaMovimientos_UsuarioId",
                table: "CajaMovimientos",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_CajaMovimientos_VentaId",
                table: "CajaMovimientos",
                column: "VentaId");

            migrationBuilder.CreateIndex(
                name: "IX_Cajas_UsuarioId",
                table: "Cajas",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Clientes_TipoDocumento_NumeroDocumento",
                table: "Clientes",
                columns: new[] { "TipoDocumento", "NumeroDocumento" },
                unique: true,
                filter: "\"NumeroDocumento\" <> ''");

            migrationBuilder.CreateIndex(
                name: "IX_Comprobantes_Serie_Numero",
                table: "Comprobantes",
                columns: new[] { "Serie", "Numero" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Comprobantes_VentaId",
                table: "Comprobantes",
                column: "VentaId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosStock_ProductoId",
                table: "MovimientosStock",
                column: "ProductoId");

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosStock_UsuarioId",
                table: "MovimientosStock",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosStock_VentaId",
                table: "MovimientosStock",
                column: "VentaId");

            migrationBuilder.CreateIndex(
                name: "IX_Pagos_CajaId",
                table: "Pagos",
                column: "CajaId");

            migrationBuilder.CreateIndex(
                name: "IX_Pagos_UsuarioId",
                table: "Pagos",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Pagos_VentaId",
                table: "Pagos",
                column: "VentaId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_NombreUsuario",
                table: "Usuarios",
                column: "NombreUsuario",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Productos_Categorias_CategoriaId",
                table: "Productos",
                column: "CategoriaId",
                principalTable: "Categorias",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Ventas_Cajas_CajaId",
                table: "Ventas",
                column: "CajaId",
                principalTable: "Cajas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Ventas_Clientes_ClienteId",
                table: "Ventas",
                column: "ClienteId",
                principalTable: "Clientes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Ventas_Usuarios_UsuarioId",
                table: "Ventas",
                column: "UsuarioId",
                principalTable: "Usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Productos_Categorias_CategoriaId",
                table: "Productos");

            migrationBuilder.DropForeignKey(
                name: "FK_Ventas_Cajas_CajaId",
                table: "Ventas");

            migrationBuilder.DropForeignKey(
                name: "FK_Ventas_Clientes_ClienteId",
                table: "Ventas");

            migrationBuilder.DropForeignKey(
                name: "FK_Ventas_Usuarios_UsuarioId",
                table: "Ventas");

            migrationBuilder.DropTable(
                name: "CajaMovimientos");

            migrationBuilder.DropTable(
                name: "Categorias");

            migrationBuilder.DropTable(
                name: "Clientes");

            migrationBuilder.DropTable(
                name: "Comprobantes");

            migrationBuilder.DropTable(
                name: "MovimientosStock");

            migrationBuilder.DropTable(
                name: "Negocios");

            migrationBuilder.DropTable(
                name: "Pagos");

            migrationBuilder.DropTable(
                name: "Cajas");

            migrationBuilder.DropTable(
                name: "Usuarios");

            migrationBuilder.DropIndex(
                name: "IX_Ventas_CajaId",
                table: "Ventas");

            migrationBuilder.DropIndex(
                name: "IX_Ventas_ClienteId",
                table: "Ventas");

            migrationBuilder.DropIndex(
                name: "IX_Ventas_IdentificadorOperacion",
                table: "Ventas");

            migrationBuilder.DropIndex(
                name: "IX_Ventas_UsuarioId",
                table: "Ventas");

            migrationBuilder.DropIndex(
                name: "IX_Productos_CategoriaId",
                table: "Productos");

            migrationBuilder.DropIndex(
                name: "IX_DetallesVenta_VentaId_ProductoId",
                table: "DetallesVenta");

            migrationBuilder.DropColumn(
                name: "CajaId",
                table: "Ventas");

            migrationBuilder.DropColumn(
                name: "ClienteId",
                table: "Ventas");

            migrationBuilder.DropColumn(
                name: "Descuento",
                table: "Ventas");

            migrationBuilder.DropColumn(
                name: "Estado",
                table: "Ventas");

            migrationBuilder.DropColumn(
                name: "IdentificadorOperacion",
                table: "Ventas");

            migrationBuilder.DropColumn(
                name: "Modalidad",
                table: "Ventas");

            migrationBuilder.DropColumn(
                name: "NumeroMesa",
                table: "Ventas");

            migrationBuilder.DropColumn(
                name: "UsuarioId",
                table: "Ventas");

            migrationBuilder.DropColumn(
                name: "Activo",
                table: "Productos");

            migrationBuilder.DropColumn(
                name: "CategoriaId",
                table: "Productos");

            migrationBuilder.DropColumn(
                name: "StockMinimo",
                table: "Productos");

            migrationBuilder.DropColumn(
                name: "NombreProducto",
                table: "DetallesVenta");

            migrationBuilder.DropColumn(
                name: "Observacion",
                table: "DetallesVenta");

            migrationBuilder.CreateIndex(
                name: "IX_DetallesVenta_VentaId_ProductoId",
                table: "DetallesVenta",
                columns: new[] { "VentaId", "ProductoId" },
                unique: true);

        }
    }
}
