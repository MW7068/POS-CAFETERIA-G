
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PosCafeteria.Infraestructura.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class ProtegerVentasHistoricas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Agregar una marca para identificar ventas históricas.
            // Las ventas nuevas tendrán EsHistorica = false.
            migrationBuilder.AddColumn<bool>(
                name: "EsHistorica",
                table: "Ventas",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            // 2. Identificar las ventas procedentes del esquema anterior.
            //
            // Estado = 0 representa que desconocemos el estado original.
            // No significa Borrador, Confirmada ni Anulada.
            //
            // Solo se marcan las ventas asociadas al usuario técnico
            // desactivado creado durante la migración anterior.
            migrationBuilder.Sql("""
                UPDATE "Ventas"
                SET "EsHistorica" = 1,
                    "Estado" = 0
                WHERE "UsuarioId" IN (
                    SELECT "Id"
                    FROM "Usuarios"
                    WHERE "NombreUsuario" = '__ventas_historicas__'
                      AND "Activo" = 0
                );
                """);

            // 3. Impedir la modificación de ventas históricas.
            migrationBuilder.Sql("""
                CREATE TRIGGER "TR_VentasHistoricas_NoActualizar"
                BEFORE UPDATE ON "Ventas"
                WHEN OLD."EsHistorica" = 1
                BEGIN
                    SELECT RAISE(
                        ABORT,
                        'No se puede modificar una venta historica'
                    );
                END;
                """);

            // 4. Impedir la eliminación de ventas históricas.
            migrationBuilder.Sql("""
                CREATE TRIGGER "TR_VentasHistoricas_NoEliminar"
                BEFORE DELETE ON "Ventas"
                WHEN OLD."EsHistorica" = 1
                BEGIN
                    SELECT RAISE(
                        ABORT,
                        'No se puede eliminar una venta historica'
                    );
                END;
                """);

            // 5. Impedir agregar detalles a una venta histórica.
            migrationBuilder.Sql("""
                CREATE TRIGGER "TR_DetallesHistoricos_NoInsertar"
                BEFORE INSERT ON "DetallesVenta"
                WHEN EXISTS (
                    SELECT 1
                    FROM "Ventas"
                    WHERE "Id" = NEW."VentaId"
                      AND "EsHistorica" = 1
                )
                BEGIN
                    SELECT RAISE(
                        ABORT,
                        'No se puede agregar un detalle historico'
                    );
                END;
                """);

            // 6. Impedir modificar detalles históricos.
            //
            // Se comprueba tanto la venta original como la nueva,
            // para impedir mover detalles hacia o desde
            // una venta histórica.
            migrationBuilder.Sql("""
                CREATE TRIGGER "TR_DetallesHistoricos_NoActualizar"
                BEFORE UPDATE ON "DetallesVenta"
                WHEN EXISTS (
                    SELECT 1
                    FROM "Ventas"
                    WHERE "Id" IN (
                        OLD."VentaId",
                        NEW."VentaId"
                    )
                    AND "EsHistorica" = 1
                )
                BEGIN
                    SELECT RAISE(
                        ABORT,
                        'No se puede modificar un detalle historico'
                    );
                END;
                """);

            // 7. Impedir eliminar detalles históricos.
            migrationBuilder.Sql("""
                CREATE TRIGGER "TR_DetallesHistoricos_NoEliminar"
                BEFORE DELETE ON "DetallesVenta"
                WHEN EXISTS (
                    SELECT 1
                    FROM "Ventas"
                    WHERE "Id" = OLD."VentaId"
                      AND "EsHistorica" = 1
                )
                BEGIN
                    SELECT RAISE(
                        ABORT,
                        'No se puede eliminar un detalle historico'
                    );
                END;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // 1. Eliminar primero los triggers creados por esta migración.
            migrationBuilder.Sql("""
                DROP TRIGGER IF EXISTS "TR_DetallesHistoricos_NoEliminar";
                DROP TRIGGER IF EXISTS "TR_DetallesHistoricos_NoActualizar";
                DROP TRIGGER IF EXISTS "TR_DetallesHistoricos_NoInsertar";
                DROP TRIGGER IF EXISTS "TR_VentasHistoricas_NoEliminar";
                DROP TRIGGER IF EXISTS "TR_VentasHistoricas_NoActualizar";
                """);

            // 2. Restaurar el valor de Estado anterior
            // a esta migración correctiva.
            //
            // ADVERTENCIA: esto revierte la protección lógica.
            // No ejecutar sobre datos operativos sin respaldo
            // y sin una evaluación previa.
            migrationBuilder.Sql("""
                UPDATE "Ventas"
                SET "Estado" = 1
                WHERE "EsHistorica" = 1
                  AND "Estado" = 0;
                """);

            // 3. Eliminar la columna incorporada por esta migración.
            migrationBuilder.DropColumn(
                name: "EsHistorica",
                table: "Ventas");
        }
    }
}
