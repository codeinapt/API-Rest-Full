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
- Los commands sin resultado implementan `IRequest` (devuelven `Unit`), y sus handlers `IRequestHandler<TCommand>`. El parámetro de `Handle` se llama `request`, como en la interfaz de Mediator.
- La carga «existe o `*_NOT_FOUND`» se comparte entre handlers de un módulo con una extensión `internal` sobre el puerto (`Areas/Commands/AreaWriteRepositoryExtensions.GetExistingByIdForUpdateAsync`, `Categories/Commands/CategoryWriteRepositoryExtensions.GetExistingByIdForUpdateAsync`).
- La carga de un **padre referenciado** reutiliza el puerto de escritura de su módulo, que ya bloquea la fila (`IAreaWriteRepository.GetByIdForUpdateAsync`), y una extensión `internal` del módulo hijo traduce «no existe» a `InvalidReferenceException(ReferencedAreaInvalid, id)` (422): `Categories/Commands/ReferencedAreaExtensions.GetReferencedAreaForUpdateAsync`. Orden en la edición: primero la entidad (404) y después el padre.

## Pipeline (equivale a SDAP-07, capa de servicios)
```
Controller → Mediator → ValidationBehavior (FluentValidation) → Handler
```
- `ValidationBehavior` ejecuta todos los `IValidator<TRequest>` y lanza `ValidationException` (→ 400).
- Validadores: `{Action}{Entity}Validator : AbstractValidator<{Action}{Entity}Command>`. Las reglas de formato repetidas en un módulo son extensiones de `IRuleBuilder` (`Areas/Commands/AreaValidationRules`: `MustBeValidAreaCode`, `MustBeValidAreaName`) que reutilizan las constantes de la entidad. Cada regla usa `Cascade(CascadeMode.Stop)` (un solo error por campo) y `OverridePropertyName(nameof(XxxRequest.Campo))`, para que la clave de `errors` sea `Code` y no `Request.Code`.
- Registro en `Program.cs`: `AddMediator` con `ServiceLifetime.Scoped`, porque los handlers dependen de servicios *scoped* (`ICurrentUser`, `IUnitOfWork`), y `PipelineBehaviors = [typeof(ValidationBehavior<,>)]`. Los handlers se registran solos (source generator). Los validadores **no**: FluentValidation.DependencyInjectionExtensions no está en la Tech Fence, así que se registran a mano en `Program.cs` como *singleton* (no tienen estado): `AddSingleton<IValidator<CreateAreaCommand>, CreateAreaValidator>()`.
- Registro manual de cada validador en `Program.cs`, justo después de `AddMediator`: `AddSingleton<IValidator<XxxCommand>, XxxValidator>()` (son *stateless*).
- Los validadores de un Command validan `command.Request.X` con `OverridePropertyName(nameof(XxxRequest.X))`, para que la clave de `errors` sea el campo del DTO (`Code`, `Name`) y no `Request.Code`, y `Cascade(CascadeMode.Stop)`, para que cada campo dé un solo error. Las reglas compartidas entre Commands de un módulo son extensiones de `IRuleBuilder` en `Application/{Module}/Commands/{Entity}ValidationRules.cs` (p. ej. `MustBeValidAreaName()`), con un mensaje `ErrorMessages.{Entity}{Field}Invalid` por campo.
- Los Commands sin respuesta implementan `IRequest` y su handler devuelve `Unit.Value`. El parámetro de `Handle` se llama `request`, como en la firma de Mediator.
- Cargar la entidad o lanzar `NotFoundException` se comparte en una extensión `internal` del puerto: `Application/{Module}/Commands/{Entity}WriteRepositoryExtensions.cs` (`GetExistingByIdForUpdateAsync`).
- El controller **no** valida ni contiene lógica: traduce HTTP ↔ Command/Query.

## Queries
- Una query lleva su objeto de parámetros, como un Command lleva su Request: `record GetAreasQuery(AreaQueryParameters Parameters) : IRequest<PagedResponse<AreaResponse>>`; el detalle, solo el id: `record GetAreaByIdQuery(int AreaId) : IRequest<AreaResponse>`.
- `{Entity}QueryParameters` es un `sealed record` que hereda de `PaginationParameters` y añade sus filtros. `Status` es `StatusFilter` (`Common/Pagination`) con valor por defecto `Active`.
- El handler delega en el puerto de lectura (`I{Entity}ReadRepository`), que devuelve ya el DTO o el `PagedResponse<T>`. El detalle resuelve el «no existe» en el handler: `area ?? throw new NotFoundException(ErrorCode.AreaNotFound, id)`. El detalle por `id` devuelve también los inactivos.
- Validador del listado (`Get{Entities}Validator : AbstractValidator<Get{Entities}Query>`): reutiliza la paginación con `RuleFor(query => query.Parameters).SetValidator(new PaginationParametersValidator()).OverridePropertyName(string.Empty)`. El `OverridePropertyName(string.Empty)` quita el prefijo `Parameters.` de FluentValidation, para que las claves de `errors` sean `Page` y `PageSize`. Los filtros propios usan `OverridePropertyName(nameof({Entity}QueryParameters.X))` y `IsInEnum()` en los enums (`ErrorMessages.StatusInvalid`).

## Controllers
- `[ApiController]`, `[Route("api/v1/{recurso}")]`, `sealed`, constructor primario con `ISender` de Mediator. Las acciones se llaman como el caso de uso sin sufijo `Async` (`GetAreas`, `GetAreaById`, `CreateArea`, `UpdateArea`, `DeactivateArea`), porque `CreatedAtAction(nameof(GetAreaById), …)` usa el nombre de la acción y MVC quita el sufijo `Async`.
- Cada acción lleva `[Authorize(Policy = PolicyNames.X)]` según `SCREEN_FLOW` §3. Un test por reflexión comprueba la política de cada acción.
- Rutas con id: `{id:int}`. Listados: `[FromQuery] {Entity}QueryParameters parameters`.
- Respuestas: `Ok(dto)` en lecturas; `CreatedAtAction(nameof(Get{Entity}ById), new { id }, dto)` en creaciones (201 + `Location`); `NoContent()` en `PUT` y `DELETE`.
- Swagger: `[ProducesResponseType<T>(status)]` por cada código de la fila de `SCREEN_FLOW` §3 (`ValidationProblemDetails` para 400, `ProblemDetails` para 404/409/422). 401 y 403 se declaran a nivel de clase. No se usa `[Produces]`: su filtro sobrescribiría el `application/problem+json` de los 400 de *model binding*.
- JSON: `JsonStringEnumConverter` en `AddControllers().AddJsonOptions(...)` (enums como texto, `DATA_MINDMAP` §4). En la *query string* los enums se enlazan sin distinguir mayúsculas (`status=active`).

## Dominio rico
- Las entidades exponen métodos con intención (`product.Deactivate(deactivation)`) y protegen sus invariantes, lanzando `DomainException` con su `ErrorCode`.
- Las fábricas de movimiento (`InventoryMovement.CreateOutbound(...)`) aplican RM-01, RM-03 y RM-08 con el snapshot del producto.
- Los límites de formato de cada regla (`VALIDATION_ERROR`) son **constantes públicas de la entidad** (`Area.CodeMinimumLength`, `Area.CodePattern`, …). El validador FluentValidation las reutiliza (→ 400) y la entidad las vuelve a comprobar como última barrera (`DomainException(ValidationError)`, que solo salta si se omite el validador). Los patrones usan `\z` en lugar de `$` para rechazar un salto de línea final.
- Las reglas que dependen de otras tablas reciben el dato ya leído por el handler: `area.Deactivate(activeCategoryCount)` (RA-03), `category.Deactivate(activeProductCount)` (RC-03). Cuando la regla depende del estado de otra entidad, se le pasa el objeto entero (regla 7): `Category.Create(area, name, createdAt)` y `category.Update(area, name, updatedAt)` lanzan `REFERENCED_AREA_INVALID {area.Id}` si el área está inactiva (RC-02). El caso "el área no existe" lo traduce el handler al mismo código, porque no hay objeto que pasar. La unicidad (`AREA_CODE_ALREADY_EXISTS`, …) la comprueba el handler a través del repositorio. En categorías se comprueba **después** del método de dominio y sobre el objeto (`EnsureNameIsAvailableAsync(category)` → `ExistsOtherActiveCategoryWithNameAsync(category)`, que excluye su propio `Id`), así la misma consulta sirve para crear (`Id = 0`) y para editar sin que la categoría choque consigo misma.
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
