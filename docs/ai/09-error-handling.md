# 09 · Manejo de errores

Catálogo canónico de códigos, estados HTTP y textos: `.sdap/DOMAIN_LOGIC.md` §6. Flujo: `.sdap/INTERFACE_FLOW.md` SEQ-08.

## Jerarquía de excepciones
| Excepción | Proyecto | HTTP |
|---|---|---|
| `ValidationException` (FluentValidation) | Application | 400 + `errors` por campo |
| `NotFoundException(ErrorCode, id)` | Application | 404 |
| `ForbiddenException(ErrorCode, params object[])` | Application | 403 (`AREA_ACCESS_DENIED`) |
| `DomainException(ErrorCode, params object[])` | Domain | 409 |
| `InvalidReferenceException(ErrorCode, id)` | Application | 422 |
| Cualquier otra | — | 500 `INTERNAL_ERROR` |

Cada excepción lleva un **`ErrorCode`**. El mapeador obtiene el status a partir del tipo de excepción, y el texto a partir de `ErrorMessages` según el código.

- `ErrorCode` (`Domain/Common`) es un `enum` en PascalCase. El `code` público se deriva del nombre en `UPPER_SNAKE` (`InsufficientStock` → `INSUFFICIENT_STOCK`), así que **renombrar un valor del enum cambia el contrato**.
- Las cuatro excepciones con código heredan de la base abstracta `InventoryException` (`Domain/Common`), que expone `Code` y `MessageArguments`. El `Message` de la excepción es el nombre del código, en inglés. Nunca se devuelve al cliente.
- `MessageArguments` rellena los placeholders del texto **por posición** y en el orden de la plantilla, por ejemplo `new DomainException(ErrorCode.InsufficientStock, stock, quantity)`.

### Argumentos por código (orden posicional)
| Código | Argumentos |
|---|---|
| `AREA_ACCESS_DENIED` | `areaCode` |
| `*_NOT_FOUND`, `AREA_INACTIVE`, `CATEGORY_INACTIVE`, `PRODUCT_INACTIVE`, `PRODUCT_ALREADY_ACTIVE`, `REFERENCED_*_INVALID`, `RELATED_MOVEMENT_INVALID` | `id` |
| `AREA_CODE_ALREADY_EXISTS` | `code` |
| `CATEGORY_NAME_ALREADY_EXISTS` | `name` |
| `SKU_ALREADY_EXISTS` | `sku` |
| `PRODUCT_HAS_STOCK` | `stock` |
| `INSUFFICIENT_STOCK` | `stock`, `quantity` (en este orden) |
| Resto | ninguno |

## Componentes
- `ErrorMessages` (`Application/Common/Messages`): constantes `public const string` en español. Los títulos llevan el sufijo `Title` y los detalles se llaman como el código. Los placeholders son **posicionales** (`{0}`, `{1}`) y el mapeador los formatea con `InvariantCulture`. **Único archivo con español** (regla 1). Los mensajes por campo de los validadores también viven aquí, con el nombre `{Entidad}{Campo}Invalid` (`AreaCodeInvalid`, `AreaNameInvalid`) o `{Campo}OutOfRange` (`PageOutOfRange`). Cada regla de FluentValidation usa `WithMessage(ErrorMessages.X)` y nunca el mensaje por defecto de la librería. Por eso los tests comparan contra estas constantes y no contra literales.
- `ErrorCatalog` + `ErrorText` (`Api/ErrorHandling`): relaciona cada `ErrorCode` con su título y su plantilla. Un test exige que todos los valores del enum tengan entrada.
- `ProblemDetailsMapper` (`Api/ErrorHandling`, estático): convierte una excepción en `ProblemDetails` con `type` (`https://httpstatuses.io/{status}`), `title`, `status`, `detail`, `code` y, en validación, `errors` (`ValidationProblemDetails`, mensajes agrupados por `PropertyName` de FluentValidation). Expone `Map(Exception)`, `MapUnauthorized()` y `MapForbidden()`. Es lógica pura y se prueba de forma unitaria.
- `ProblemDetailsResponseWriter`: fija el status y escribe `application/problem+json` con el tipo real del objeto, para no perder `errors`. No depende de la cabecera `Accept`.
- `GlobalExceptionHandler : IExceptionHandler`: usa el mapeador y el writer. Registra en el log con nivel `Error` solo los 500 y con `Information` el resto. Necesita `AddProblemDetails()` y `app.UseExceptionHandler()` como primer middleware.
- 401 y 403 de autenticación y políticas: `ProblemDetailsAuthorizationResultHandler : IAuthorizationMiddlewareResultHandler` delega en el handler por defecto, que hace el challenge o el forbid de JwtBearer y conserva `WWW-Authenticate`. Después escribe el ProblemDetails (`UNAUTHORIZED`, `FORBIDDEN`). Los eventos de JwtBearer no se tocan.
- Mensajes de validación por campo: `ErrorMessages.{Entity}{Field}Invalid` (`AreaCodeInvalid`, `AreaNameInvalid`, `ProductSkuInvalid`, `ProductNameInvalid`, `ProductDescriptionInvalid`; `MinimumStockInvalid` sin prefijo porque el campo ya es único; en movimientos `MovementTypeInvalid`, `MovementQuantityInvalid` y `MovementNotesTooLong`), uno por campo aunque falle más de una regla (`CascadeMode.Stop`). Las claves de `errors` son los nombres de las propiedades del DTO en PascalCase (`Code`, `Name`, `Page`).
- `ValidationBehavior<TMessage, TResponse>` (`Application/Common/Behaviors`): ejecuta en secuencia todos los `IValidator<TMessage>`, junta los fallos y lanza `FluentValidation.ValidationException`. Si no hay fallos, llama al handler.
- **Errores de *model binding*** (`status=foo`, `page=abc`, JSON mal formado): fallan antes de llegar a Mediator. `[ApiController]` los envía a `ApiBehaviorOptions.InvalidModelStateResponseFactory`, que en `Program.cs` apunta a `InvalidModelStateResponseFactory.Create` (`Api/ErrorHandling`). Devuelve un `ObjectResult` 400 `application/problem+json` con `ProblemDetailsMapper.MapInvalidModelState(modelState)`: código `VALIDATION_ERROR` y, por cada campo con error, un único mensaje `ErrorMessages.FieldValueInvalid` (los mensajes del framework están en inglés y no se exponen). La constante `ProblemDetailsResponseWriter.ProblemJsonContentType` es pública para reutilizarla.
- Un valor de enum fuera de la lista blanca (`status=7`, `sortBy=price`, `sortDirection=9`) ya lo rechaza el *model binding*. `IsInEnum()` con `ErrorMessages.StatusInvalid` (y en productos `SortByInvalid` y `SortDirectionInvalid`) queda como segunda barrera del validador, para quien envíe la query sin pasar por HTTP.

## Reglas
- Los handlers **no** capturan excepciones para devolver códigos HTTP: lanzan y el handler global traduce.
- Prohibido devolver trazas, SQL o nombres internos en `detail`.
- Prohibido crear códigos de error que no estén en `DOMAIN_LOGIC` §6.
