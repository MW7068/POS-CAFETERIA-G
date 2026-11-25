
# PBI-03 – Tratamiento de ventas históricas

## Problema

Las ventas del esquema InicialPersistencia no guardaban
un estado de venta ni un usuario responsable.

Durante la migración no es posible determinar si una
venta antigua estaba en borrador, confirmada o anulada.

Por tanto, no debe considerarse automáticamente un
borrador editable.

## Decisión

1. Conservar las ventas y sus detalles originales.
2. Asignar las ventas anteriores al usuario técnico
   desactivado `__ventas_historicas__`.
3. Incorporar la marca de persistencia `EsHistorica`.
4. Representar el estado desconocido con el valor
   técnico `0` en SQLite.
5. No agregar nuevos estados al enum del dominio
   desarrollado por Brayan.
6. Impedir mediante triggers SQLite la modificación
   o eliminación de ventas históricas y sus detalles.
7. Mantener las ventas nuevas con `EsHistorica = false`.

## Migraciones

- InicialPersistencia: esquema anterior.
- ModeloCompletoPbi03: incorpora el nuevo modelo.
- ProtegerVentasHistoricas: identifica y protege
  las ventas anteriores.

Las migraciones pendientes deben aplicarse como
una actualización completa. No debe desplegarse
ModeloCompletoPbi03 de forma aislada, porque su
valor predeterminado temporal para Estado es 1.

## Validación

MigracionHistoricaTests comprueba:

- Conservación de productos, ventas y detalles.
- Estado histórico desconocido.
- Usuario técnico desactivado.
- Protección ante edición y eliminación.
- Funcionamiento de ventas nuevas.
- Integridad de claves foráneas.
- PRAGMA integrity_check = ok.

## Restricciones de despliegue

No aplicar las migraciones a la base SQLite operativa
sin respaldo verificable y autorización del equipo.

La reversión de ProtegerVentasHistoricas elimina
los bloqueos históricos, por lo que no se considera
una operación segura de producción sin revisión.

La integración se realizará primero en la rama
feature/pbi-03-dominio, mediante el PR correspondiente.

Este trabajo no cierra los Issues funcionales.
