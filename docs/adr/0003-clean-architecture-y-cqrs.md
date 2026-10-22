# ADR-0003: Clean Architecture + CQRS (EF Core lecturas / Dapper escrituras)

- **Estado:** Aceptada
- **Fecha:** 2026-10-22

## Contexto
El enunciado exige Entity Framework para lecturas y Dapper para escrituras, y sugiere CQRS. Además se evalúan SOLID, Clean Code y diseño de la solución.

## Decisión
- **Clean Architecture en 4 proyectos:** `Domain`, `Application`, `Infrastructure`, `Api`. Las dependencias apuntan hacia el dominio.
- **CQRS con la librería Mediator** (martinothamar/Mediator, MIT, generada en compilación). *Revisión 2026-10-22: inicialmente se eligió MediatR, pero desde la v13 (julio 2025) tiene licencia comercial; se sustituye con el mismo criterio de licencias de ADR-0012. La forma de la API es equivalente (`IRequest`, handlers, pipeline behaviors).*
  - **Commands** → handlers que escriben con **Dapper**.
  - **Queries** → handlers que leen con **EF Core** (`AsNoTracking()` y proyección directa a DTO; EF nunca escribe).
- Validación y comportamientos transversales mediante *pipeline behaviors* de Mediator.
- Los Commands también leen con Dapper el estado que necesitan para decidir la escritura, dentro de la misma transacción. "Lecturas con EF" se refiere al lado Query de CQRS.

## Alternativas
- **Arquitectura en capas tradicional:** más simple, pero acopla el dominio a la infraestructura.
- **Vertical Slices:** válida, pero menos alineada con lo que se evalúa (SOLID, separación explícita de capas).

## Consecuencias
- La exigencia EF/Dapper deja de ser un capricho técnico y encaja de forma natural en la separación Command/Query.
- Dos tecnologías de acceso a datos sobre el mismo esquema: el esquema debe mantenerse coherente (scripts SQL versionados aplicados con DbUp, ver ADR-0014).
