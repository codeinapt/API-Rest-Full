# ADR-0004: Integridad referencial sin FOREIGN KEYS

- **Estado:** Aceptada
- **Fecha:** 2026-10-22

## Contexto
El enunciado prohíbe usar FOREIGN KEYS. La base de datos no impedirá referencias huérfanas.

## Decisión
La integridad se garantiza en la aplicación:
- **Existencia:** antes de crear o modificar una referencia (producto → categoría, categoría → área) se valida que el destino exista y esté activo.
- **Operaciones atómicas:** las escrituras críticas incluyen la condición de existencia en la propia sentencia (por ejemplo `UPDATE ... WHERE Id = @Id AND IsActive = 1`) y verifican las filas afectadas.
- **Borrado:** soft delete (ver ADR-0006) para que el histórico nunca quede huérfano.
- **Unicidad:** se usan índices `UNIQUE` (no son FOREIGN KEYS). Con soft delete se usan **índices filtrados**: `CREATE UNIQUE INDEX ... WHERE IsActive = 1`.

## Consecuencias
- Las reglas de integridad se vuelven reglas de dominio explícitas y probables con tests unitarios.
- Riesgo residual: escrituras externas a la API directamente sobre la BD podrían romper la integridad. Se asume, porque la API es el único punto de escritura.
