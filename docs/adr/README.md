# Architecture Decision Records (ADR)

Registro de las decisiones tomadas durante la fase de **Inception dialéctica** de SDAP (*Spec-Driven Agentic Programming*). Cada ADR documenta el contexto, las alternativas evaluadas, la decisión y sus consecuencias, para que cualquier decisión pueda ser trazada y defendida.

Formato: Michael Nygard (Estado · Contexto · Decisión · Alternativas · Consecuencias).

| # | Decisión | Estado |
|---|---|---|
| [0000](0000-metodologia-sdap-y-adrs.md) | Metodología SDAP y registro de decisiones con ADRs | Aceptada |
| [0001](0001-dotnet-10-lts.md) | .NET 10 LTS como plataforma | Aceptada |
| [0002](0002-keycloak-como-servidor-oauth2.md) | Keycloak como servidor de autorización OAuth2 | Aceptada |
| [0003](0003-clean-architecture-y-cqrs.md) | Clean Architecture + CQRS (EF Core lecturas / Dapper escrituras) | Aceptada |
| [0004](0004-integridad-sin-foreign-keys.md) | Integridad referencial sin FOREIGN KEYS | Aceptada |
| [0005](0005-stock-materializado-y-libro-de-movimientos.md) | Stock materializado + libro de movimientos inmutable | Aceptada |
| [0006](0006-soft-delete-desactivacion-y-reactivacion.md) | Soft delete, desactivación con motivo y reactivación | Aceptada |
| [0007](0007-ajustes-de-inventario-por-delta.md) | Ajustes de inventario por delta | Aceptada |
| [0008](0008-modelo-organizativo-area-categoria-producto.md) | Modelo organizativo Área → Categoría → Producto | Aceptada |
| [0009](0009-autorizacion-roles-alcance-por-area.md) | Autorización por roles, alcance por área y separación de funciones | Aceptada |
| [0010](0010-errores-problemdetails-en-espanol.md) | Errores con ProblemDetails (RFC 9457) y mensajes en español | Aceptada |
| [0011](0011-consultas-paginacion-y-filtros.md) | Consultas: paginación, filtros y punto de reorden | Aceptada |
| [0012](0012-estrategia-de-pruebas.md) | Estrategia de pruebas y TDD | Aceptada |
| [0013](0013-flujo-de-trabajo-con-ia.md) | Flujo de trabajo con IA | Aceptada |

Limitaciones conocidas y mejoras futuras identificadas: [IMPROVEMENTS.md](../IMPROVEMENTS.md).
