# 03 · Patrones de arquitectura (incluye SDAP-06 y SDAP-07)

## Clean Architecture
Las dependencias apuntan hacia `Domain`. `Application` define **puertos** (interfaces) e `Infrastructure` y `Api` los implementan. La composición (DI) ocurre solo en `Api/Program.cs`, más los `DependencyInjection.cs` de cada proyecto.

## CQRS con Mediator
| Lado | Tipo | Implementa | Acceso a datos |
|---|---|---|---|
| Lectura | `record XxxQuery : IRequest<TResponse>` | `IRequestHandler<XxxQuery, TResponse>` | `I*ReadRepository` → EF Core |
| Escritura | `record XxxCommand : IRequest<TResponse>` | `IRequestHandler<XxxCommand, TResponse>` | `I*WriteRepository` + `IUnitOfWork` → Dapper |

- Un Command **contiene el objeto Request** más los datos de la ruta: `record CreateProductCommand(CreateProductRequest Request)`, `record RegisterMovementCommand(int ProductId, RegisterMovementRequest Request)`. Regla de Clean Code 7.
- Los handlers **orquestan**: cargan el estado, delegan las reglas en el dominio, persisten y mapean la respuesta. No contienen reglas de negocio.

## Pipeline (equivale a SDAP-07, capa de servicios)
```
Controller → Mediator → ValidationBehavior (FluentValidation) → Handler
```
- `ValidationBehavior` ejecuta todos los `IValidator<TRequest>` y lanza `ValidationException` (→ 400).
- Registro en `Program.cs`: `AddMediator` con `ServiceLifetime.Scoped`, porque los handlers dependen de servicios *scoped* (`ICurrentUser`, `IUnitOfWork`), y `PipelineBehaviors = [typeof(ValidationBehavior<,>)]`. Los handlers se registran solos (source generator). Los validadores **no**: FluentValidation.DependencyInjectionExtensions no está en la Tech Fence, así que se registran a mano.
- El controller **no** valida ni contiene lógica: traduce HTTP ↔ Command/Query.

## Dominio rico
- Las entidades exponen métodos con intención (`product.Deactivate(deactivation)`) y protegen sus invariantes, lanzando `DomainException` con su `ErrorCode`.
- Las fábricas de movimiento (`InventoryMovement.CreateOutbound(...)`) aplican RM-01, RM-03 y RM-08 con el snapshot del producto.
- Los límites de formato de cada regla (`VALIDATION_ERROR`) son **constantes públicas de la entidad** (`Area.CodeMinimumLength`, `Area.CodePattern`, …). El validador FluentValidation las reutiliza (→ 400) y la entidad las vuelve a comprobar como última barrera (`DomainException(ValidationError)`, que solo salta si se omite el validador). Los patrones usan `\z` en lugar de `$` para rechazar un salto de línea final.
- Las reglas que dependen de otras tablas reciben el dato ya leído por el handler: `area.Deactivate(activeCategoryCount)` (RA-03). La unicidad (`AREA_CODE_ALREADY_EXISTS`, …) la comprueba el handler a través del repositorio.
- Las entidades tienen un constructor privado sin parámetros y *setters* privados. Se crean con la fábrica `Create` y Dapper puede materializarlas.

## Doble barrera (regla + concurrencia)
1. El **dominio** valida con el estado leído, lo que da errores comprensibles.
2. La **sentencia SQL** repite la condición crítica de forma atómica (`WHERE Stock + @Delta >= 0`). Si afecta 0 filas, el repositorio informa al handler y este lanza el error correspondiente. Detalle: `.sdap/INTERFACE_FLOW.md` SEQ-04.

## Flujo de datos (SDAP-06)
Los diagramas de secuencia canónicos están en `.sdap/INTERFACE_FLOW.md`:
- SEQ-02: query genérica;
- SEQ-03: command genérico;
- SEQ-04 a SEQ-07: operaciones de inventario;
- SEQ-08: errores.

## Mapeo
Manual. Métodos de extensión estáticos `ToResponse()` en `Application/{Module}/Dtos/{Entity}Mappings.cs`. En lecturas EF, la proyección se hace directamente en el `Select` hacia el DTO.

## Tiempo
`IClock.UtcNow` en lugar de `DateTime.UtcNow`, para que los tests sean deterministas. El dominio no conoce `IClock`: el handler le pasa el instante como `DateTime` (`Area.Create(code, name, clock.UtcNow)`).
