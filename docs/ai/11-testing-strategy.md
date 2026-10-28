# 11 · Estrategia de pruebas

Decisión: ADR-0012.

## Pirámide
| Nivel | Proyecto | Qué prueba | Dependencias |
|---|---|---|---|
| Unitario: dominio | `Inventory.Domain.Tests` | Métodos de entidades y fábricas; cada regla RA, RC, RP y RM | Ninguna |
| Unitario: aplicación | `Inventory.Application.Tests` | Handlers (orquestación, errores), validadores, `AreaAccessPolicy` | NSubstitute para los puertos |
| Unitario: API | `Inventory.Api.Tests` | `ProblemDetailsMapper`, `InvalidModelStateResponseFactory`, `ClaimsCurrentUser`, `KeycloakRolesClaimsTransformation`, controllers (resultado HTTP, mensaje enviado y política de cada acción por reflexión) | NSubstitute para `ISender` |
| Integración *(extra)* | `Inventory.IntegrationTests` | SQL de Dapper (atomicidad, concurrencia), consultas EF, scripts DbUp | Testcontainers (SQL Server) |

## TDD
1. **Rojo:** escribir el test de la regla y commit `test(T-XX): <regla> ...`.
2. **Verde:** el código mínimo para pasarlo y commit `feat(T-XX): ...`.
3. **Refactor:** con los tests en verde, commit `refactor(T-XX): ...` si aplica.

## Convenciones
- **Nombre:** `MethodName_Scenario_ExpectedResult`, en inglés. Ejemplo: `Deactivate_WhenReasonIsOtherWithoutNotes_ThrowsValidationError`.
- **Estructura AAA** (Arrange, Act, Assert). La línea en blanco entre bloques **no** está permitida (regla 2): los bloques se separan extrayendo builders o *helpers* si hace falta claridad.
- **Un comportamiento por test.** `[Theory]` para variaciones del mismo comportamiento.
- **Builders de prueba** (`ProductBuilder`) para no repetir datos válidos.
- **`Id` persistido:** las entidades tienen `Id` con *setter* privado (lo asigna Dapper). En los tests de dominio, `PersistedIdentity.Assign(entity, id)` (`tests/Inventory.Domain.Tests/Common/`) simula ese `Id`, para que los asserts sobre `AreaId` o `MessageArguments` comprueben un valor real y no `0`.
- **Asserts con Shouldly** (`result.ShouldBe(...)`, `Should.Throw<DomainException>(...)`), comprobando también el `ErrorCode`.
- **Tiempo:** `IClock` simulado.

## Integración (`Inventory.IntegrationTests`)
- **Requiere Docker.** Forma parte de `Inventory.sln`, así que `dotnet test` en la raíz la ejecuta (unos 30 s, casi todo el arranque del contenedor).
- **`SqlServerFixture`** (*assembly fixture* de xUnit v3): un contenedor SQL Server por ejecución, migrado con `DatabaseMigrator` (scripts DbUp + *seed*). Crea un área `ITEST` y una categoría compartidas; cada test crea sus propios productos con SKU único (`InventoryOperations.CreateUniqueSku()`), así que no hace falta limpiar la base entre tests.
- **Sin HTTP:** `AddInventoryInfrastructure` + `ActivatorUtilities` instancian los **handlers públicos reales** (`RegisterMovementHandler`, `DeleteProductHandler`…), con `IntegrationTestUser` como `ICurrentUser`. Se prueba el flujo del caso de uso y el SQL de Dapper sin `InternalsVisibleTo`. Para atacar una sentencia SQL concreta sin la comprobación previa del handler, `WriteScope` abre una transacción y expone los puertos de escritura.
- **Concurrencia determinista:** `ProductRowLock` abre una transacción que retiene `UPDLOCK, HOLDLOCK` sobre la fila del producto. El test lanza las operaciones, espera a que `sys.dm_exec_requests` muestre N sesiones esperando un bloqueo (`LCK_M_*`) y libera la fila. SQL Server concede los bloqueos en orden FIFO, así que el test fija qué operación va primero y siempre recorre el camino de carrera (UPDATE con 0 filas), sin `Task.Delay`. Por eso la paralelización entre tests está desactivada (`ParallelMode.None`): el conteo de esperas solo debe ver las sesiones del test en curso.
- **Qué se cubre** (casos límite de `INTERFACE_FLOW.md`):
  - RM-03: dos salidas en cola que juntas superan el stock (una aplica, la otra `INSUFFICIENT_STOCK` con el stock real) y una ráfaga de 20 salidas concurrentes que termina en 0.
  - `DELETE` frente a entrada, en ambos órdenes (`PRODUCT_HAS_STOCK` / `PRODUCT_INACTIVE`), y la guarda `AND Stock = 0` con una instantánea obsoleta.
  - Índice único filtrado de SKU: inserción y reactivación duplicadas (2601 → `SKU_ALREADY_EXISTS`), reutilización del SKU de un producto inactivo y dos altas concurrentes.
  - RM-11: `Stock = Σ Delta` tras entrada, salida y ajuste, y tras la ráfaga, con la cadena `PreviousStock` = `ResultingStock` anterior.
  - DbUp: re-ejecutar no añade entradas al journal, y el *seed* cuadra el libro.
- **Mutación comprobada (T-24):** quitar `AND Stock + @Delta >= 0` de `ApplyDeltaSql` o `AND Stock = 0` de `DeleteSql` hace fallar 3 tests.

## Cobertura
- coverlet con objetivo de **≥ 80 %** en `Domain` y `Application`. `Infrastructure` y `Program.cs` se excluyen del objetivo unitario (los cubre la integración).
- **CI (`.github/workflows/ci.yml`, GitHub Actions):** en cada *push*, en los PR hacia `develop`/`main` y a demanda. Compila en Release (warnings = errores) y ejecuta **todos** los proyectos de prueba, integración incluida (el runner `ubuntu-latest` trae Docker para Testcontainers).
- **Informe:** ReportGenerator fusiona los XML Cobertura de los cuatro proyectos de prueba en un informe HTML, que se sube como artefacto `coverage-report` junto con los `.trx`. El resumen aparece en el *Job Summary* de la ejecución.
- **Umbral:** el job falla si la cobertura de líneas de `Inventory.Domain` + `Inventory.Application` (fusionando todas las suites) baja del 80 % (`MINIMUM_LINE_COVERAGE`). En T-25 estaba en el 96,7 %.

## Matriz mínima obligatoria
Cada regla de `DOMAIN_LOGIC` con un `Error` asociado tiene **al menos un test** que provoca ese error y comprueba su `ErrorCode`.
