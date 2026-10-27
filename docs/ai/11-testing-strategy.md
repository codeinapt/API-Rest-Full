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
- **Asserts con Shouldly** (`result.ShouldBe(...)`, `Should.Throw<DomainException>(...)`), comprobando también el `ErrorCode`.
- **Tiempo:** `IClock` simulado.

## Cobertura
- coverlet con objetivo de **≥ 80 %** en `Domain` y `Application`. `Infrastructure` y `Program.cs` se excluyen del objetivo unitario (los cubre la integración).
- El informe se publica como artefacto en CI.

## Matriz mínima obligatoria
Cada regla de `DOMAIN_LOGIC` con un `Error` asociado tiene **al menos un test** que provoca ese error y comprueba su `ErrorCode`.
