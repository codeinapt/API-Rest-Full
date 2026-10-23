# 09 · Manejo de errores

Catálogo canónico de códigos, estados HTTP y textos: `.sdap/DOMAIN_LOGIC.md` §6. Flujo: `.sdap/INTERFACE_FLOW.md` SEQ-08.

## Jerarquía de excepciones
| Excepción | Proyecto | HTTP |
|---|---|---|
| `ValidationException` (FluentValidation) | Application | 400 + `errors` por campo |
| `NotFoundException(ErrorCode, id)` | Application | 404 |
| `ForbiddenException(ErrorCode)` | Application | 403 (`AREA_ACCESS_DENIED`) |
| `DomainException(ErrorCode, ...)` | Domain | 409 |
| `InvalidReferenceException(ErrorCode, id)` | Application | 422 |
| Cualquier otra | — | 500 `INTERNAL_ERROR` |

Cada excepción lleva un **`ErrorCode`**. El mapeador obtiene el status a partir del tipo de excepción, y el texto a partir de `ErrorMessages` según el código.

## Componentes
- `ErrorMessages` (`Application/Common/Messages`): constantes `public const string` en español, con `{placeholders}` formateados por el mapeador. **Único archivo con español** (regla 1).
- `ProblemDetailsMapper` (`Api/ErrorHandling`): convierte una excepción en `ProblemDetails` con `type`, `title`, `status`, `detail`, `code` y, en validación, `errors`. Es lógica pura y se prueba de forma unitaria.
- `GlobalExceptionHandler : IExceptionHandler`: usa el mapeador, registra en el log (con nivel `Error` solo para los 500) y escribe `application/problem+json`.
- 401 y 403 de autenticación y políticas: se personalizan los eventos de JwtBearer y `IAuthorizationMiddlewareResultHandler` para que también respondan con ProblemDetails (`UNAUTHORIZED`, `FORBIDDEN`).

## Reglas
- Los handlers **no** capturan excepciones para devolver códigos HTTP: lanzan y el handler global traduce.
- Prohibido devolver trazas, SQL o nombres internos en `detail`.
- Prohibido crear códigos de error que no estén en `DOMAIN_LOGIC` §6.
