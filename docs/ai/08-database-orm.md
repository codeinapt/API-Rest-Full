# 08 · Base de datos, EF Core y Dapper

Esquema canónico: `.sdap/DATA_MINDMAP.md` §2.

## Reglas inquebrantables
- **Prohibido `FOREIGN KEY`.** Permitidos `UNIQUE` (filtrados por `IsActive = 1`) y `CHECK`.
- **EF Core solo lee.** Contexto `InventoryReadDbContext`:
  - `QueryTrackingBehavior.NoTracking` global;
  - sin `DbSet` expuestos fuera de `Persistence/Read`;
  - **nunca** `SaveChanges`.
- **Dapper solo escribe** (y lee el estado necesario dentro de la transacción del Command).
- **Esquema solo con DbUp.** Nada de migraciones EF ni `EnsureCreated`.

## EF Core (lecturas)
- Las configuraciones (`IEntityTypeConfiguration<T>`) mapean tablas y columnas existentes. **No** se declaran relaciones de navegación con FK; los *joins* se escriben explícitos en LINQ.
- `HasQueryFilter(x => x.IsActive)` **no** se usa globalmente: el filtro `status` (`active | inactive | all`) se aplica de forma explícita en cada consulta, porque los detalles por `id` también devuelven inactivos.
- Proyección directa a DTO en el `Select`. Paginación con `Skip/Take`, más `CountAsync` sobre la misma consulta filtrada.
- `sortBy` se resuelve con una lista blanca (`switch` a expresiones), nunca con texto dinámico.

## Dapper (escrituras)
- `SqlConnectionFactory` crea la conexión a partir de `INVENTORY_DB_CONNECTION`.
- `IUnitOfWork` abre la conexión y la transacción. Los repositorios de escritura reciben la conexión y la transacción del `UnitOfWork`.
- **Siempre parametrizado** (`@Param`). Prohibido concatenar SQL.
- Las sentencias críticas son **condicionales** y se comprueban las filas afectadas:
  ```sql
  UPDATE Products SET Stock = Stock + @Delta, UpdatedAt = @Now
  OUTPUT deleted.Stock AS PreviousStock, inserted.Stock AS ResultingStock
  WHERE Id = @Id AND IsActive = 1 AND Stock + @Delta >= 0;
  ```
- Validación del padre en escrituras de catálogo: `SELECT ... FROM Categories WITH (UPDLOCK, HOLDLOCK) WHERE Id = @Id AND IsActive = 1`.
- Las violaciones de índice único (`SqlException.Number` 2601/2627) se traducen a `*_ALREADY_EXISTS`.
- Los enums se envían como `(byte)` para encajar con `TINYINT`.

## DbUp
- Scripts en `Infrastructure/Migrations/Scripts/NNNN_Description.sql` como **recurso embebido**, ejecutados en orden por nombre.
- `DatabaseMigrator` se ejecuta al arrancar la API: crea la BD si no existe y aplica los scripts pendientes (journal `SchemaVersions`).
- **Un script commiteado no se edita jamás.** Los cambios se hacen con un script nuevo.
- Los scripts son idempotentes en la medida de lo posible (`IF NOT EXISTS`).

## Índices
Los de `DATA_MINDMAP` §2. Un índice nuevo requiere su propio script y actualizar `DATA_MINDMAP` (lo edita el humano).
