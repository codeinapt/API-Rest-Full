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
- EF **no** mapea las entidades de dominio, sino filas de solo lectura propias de la persistencia (`Persistence/Read/Models/{Table}Row`, `internal`), una por tabla, con las columnas de `DATA_MINDMAP` §2. Los enums se leen como `byte` y el repositorio los convierte al proyectar. Así el dominio no necesita constructores pensados para EF.
- `InventoryReadDbContext` (`internal`) fija `NoTracking` en `OnConfiguring`, aplica las configuraciones del ensamblado y convierte todo `DateTime` a `DateTimeKind.Utc` (`UtcDateTimeConverter`). `SaveChanges` y `SaveChangesAsync` lanzan `InvalidOperationException`.
- Las configuraciones fijan tabla, clave, longitudes e `IsUnicode(false)` en las columnas `VARCHAR` (`Code`, `Sku`), para que los parámetros no se envíen como `nvarchar` y se usen los índices.
- Las configuraciones (`IEntityTypeConfiguration<T>`) mapean tablas y columnas existentes. **No** se declaran relaciones de navegación con FK; los *joins* se escriben explícitos en LINQ.
- `HasQueryFilter(x => x.IsActive)` **no** se usa globalmente: el filtro `status` (`active | inactive | all`) se aplica de forma explícita en cada consulta, porque los detalles por `id` también devuelven inactivos.
- Proyección directa a DTO en el `Select`. Paginación con `Skip/Take`, más `CountAsync` sobre la misma consulta filtrada.
- `sortBy` se resuelve con una lista blanca (`switch` a expresiones), nunca con texto dinámico.

## Dapper (escrituras)
- `SqlConnectionFactory` crea la conexión a partir de `INVENTORY_DB_CONNECTION`.
- `IUnitOfWork` abre la conexión y la transacción. Los repositorios de escritura reciben la conexión y la transacción del `UnitOfWork`.
  - Puerto (`Application`): `BeginTransactionAsync(ct)` y `CommitAsync(ct)`.
  - `UnitOfWork` (`Infrastructure`, *scoped*, `internal`) expone `Connection` y `Transaction` a los repositorios de escritura, que dependen de la clase concreta. Usarlos antes de `BeginTransactionAsync`, o empezar dos transacciones, lanza `InvalidOperationException`.
  - **Rollback:** si el handler lanza antes de `CommitAsync`, el `DisposeAsync` del ámbito libera la `SqlTransaction` sin confirmar y SQL Server la revierte. Los handlers no necesitan `try/catch`.
- Repositorios de escritura: `internal sealed class {Entity}WriteRepository(UnitOfWork unitOfWork)`, registrados como *scoped* en `DependencyInjection.cs`. El SQL va en constantes `private const string` (raw string literals) y cada llamada usa `CommandDefinition` con `unitOfWork.Transaction` y el `CancellationToken`.
- **Carga para modificar:** `GetByIdForUpdateAsync` lee la fila `WITH (UPDLOCK, HOLDLOCK)` y Dapper materializa la entidad de dominio (constructor privado y *setters* privados). El bloqueo serializa, por ejemplo, la desactivación de un área (RA-03) frente a la creación de una categoría en ella, que bloquea la misma fila del padre. Con la fila bloqueada, el `UPDATE` posterior filtra solo por `Id`.
- **Inserción:** `INSERT ... OUTPUT inserted.<columnas> VALUES (...)` con `QuerySingleAsync<Entidad>`. Devuelve la entidad persistida con su `Id` de identidad, y el handler mapea esa instancia.
- Los parámetros de columnas `VARCHAR` (`Code`, `Sku`) se envían como `DbString { IsAnsi = true, Length = <máximo> }`, igual que `IsUnicode(false)` en EF, para que se use el índice.
- Repositorio de escritura: `internal sealed class {Entity}WriteRepository(UnitOfWork unitOfWork)`, registrado *scoped* en `Infrastructure/DependencyInjection.cs`. El SQL va en constantes `private const string` (*raw string literals*) y cada llamada usa `CommandDefinition` con `unitOfWork.Transaction` y el `CancellationToken`.
- Puerto de escritura por entidad (ejemplo `IAreaWriteRepository`): `GetByIdForUpdateAsync` (carga la entidad `WITH (UPDLOCK, HOLDLOCK)` para serializar las reglas que dependen de otras tablas, p. ej. RA-03 frente a la creación de categorías), las comprobaciones (`ExistsActiveCodeAsync`, `CountActiveCategoriesAsync`), `InsertAsync(entity)` y `UpdateAsync(entity)`.
- `InsertAsync` devuelve la entidad persistida materializada por Dapper desde `OUTPUT inserted.*`, para que la respuesta lleve el `Id` de identidad sin una segunda lectura.
- Las columnas `VARCHAR` (`Code`, `Sku`) se envían como `DbString { IsAnsi = true, Length = ... }`, para evitar la conversión implícita y usar el índice.
- **Siempre parametrizado** (`@Param`). Prohibido concatenar SQL.
- Las sentencias críticas son **condicionales** y se comprueban las filas afectadas:
  ```sql
  UPDATE Products SET Stock = Stock + @Delta, UpdatedAt = @Now
  OUTPUT deleted.Stock AS PreviousStock, inserted.Stock AS ResultingStock
  WHERE Id = @Id AND IsActive = 1 AND Stock + @Delta >= 0;
  ```
- Validación del padre en escrituras de catálogo: `SELECT ... FROM Categories WITH (UPDLOCK, HOLDLOCK) WHERE Id = @Id AND IsActive = 1`.
- Las violaciones de índice único (`SqlException.Number` 2601/2627) se traducen a `*_ALREADY_EXISTS`: el repositorio las captura con `catch ... when` y lanza `DomainException` (p. ej. `AreaCodeAlreadyExists` con el código). Es la segunda barrera: el handler ya comprobó antes la unicidad con `ExistsActive*Async`.
- Los enums se envían como `(byte)` para encajar con `TINYINT`.

## Paginación
- `PaginationParameters` (`Application/Common/Pagination`): `record` no sellado con `Page` (1) y `PageSize` (20). Los `*QueryParameters` heredan de él. `CalculateItemsToSkip()` da el valor de `Skip`.
- `PaginationParametersValidator`: `Page ≥ 1` y `PageSize` entre 1 y 100, con `ErrorMessages.PageOutOfRange` y `ErrorMessages.PageSizeOutOfRange`. Los validadores de cada query lo reutilizan con `Include(new PaginationParametersValidator())`.
- `PagedResponse<T>(items, pagination, totalCount)` calcula `TotalPages` redondeando hacia arriba (0 si no hay elementos).

## DbUp
- Scripts en `Infrastructure/Migrations/Scripts/NNNN_Description.sql` como **recurso embebido**, ejecutados en orden por nombre.
- `DatabaseMigrator` se ejecuta al arrancar la API: crea la BD si no existe y aplica los scripts pendientes (journal `SchemaVersions`).
- **Un script commiteado no se edita jamás.** Los cambios se hacen con un script nuevo.
- Los scripts son idempotentes en la medida de lo posible (`IF NOT EXISTS`).
- Scripts actuales: `0001_CreateAreas`, `0002_CreateCategories`, `0003_CreateProducts`, `0004_CreateInventoryMovements` (una tabla por script, con sus índices). El journal guarda el nombre completo del recurso (`Inventory.Infrastructure.Migrations.Scripts.0001_CreateAreas.sql`): **no se renombra** ni se mueve un script ya commiteado.
- Nombres de restricciones: `PK_{Tabla}`, `DF_{Tabla}_{Columna}`, `CK_{Tabla}_{Columna}_{Regla}` (p. ej. `CK_InventoryMovements_ResultingStock_MatchesDelta`), y los índices como en `DATA_MINDMAP` §2.
- `AdjustmentReason` obligatorio en ajustes: `CHECK (Type <> 3 OR AdjustmentReason IS NOT NULL)` (`MovementType.Adjustment = 3`).
- Arranque: `Program.cs` lee `INVENTORY_DB_CONNECTION` (`EnvironmentVariableNames.DatabaseConnection`), llama a `AddInventoryInfrastructure(connectionString)` y, tras `builder.Build()`, ejecuta `DatabaseMigrator.Migrate()` antes de servir peticiones.
- `DatabaseMigrator`: `EnsureDatabase` + `DeployChanges` con una transacción por script y log a `ILogger<DatabaseMigrator>`. Si un script falla lanza `InvalidOperationException` con el nombre del script, y la API no arranca.
- Los scripts se prueban contra SQL Server real en `Inventory.IntegrationTests` (T-24), no con tests unitarios.

## Índices
Los de `DATA_MINDMAP` §2. Un índice nuevo requiere su propio script y actualizar `DATA_MINDMAP` (lo edita el humano).
