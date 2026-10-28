# Capa 1 — Contexto vivo de código (`docs/ai/`)

Mapa operativo del código para los agentes. A diferencia de `.sdap/` (inmutable), estos archivos **se actualizan con cada tarea** (*Reverse Sync*).

## Aplicación proporcional del estándar SDAP

SDAP define 15 archivos. Para una API de 4 entidades sin interfaz gráfica, se mantienen la numeración y la trazabilidad, pero se fusionan o descartan los que no aportan, para no generar documentación sin uso (y tokens sin valor):

| # | Archivo SDAP | Estado |
|---|---|---|
| 00 | `project-overview.md` | ✅ [00-project-overview.md](00-project-overview.md) |
| 01 | `tech-stack.md` | ✅ [01-tech-stack.md](01-tech-stack.md) (incluye 12) |
| 02 | `folder-structure.md` | ✅ [02-folder-structure.md](02-folder-structure.md) |
| 03 | `architecture-patterns.md` | ✅ [03-architecture-patterns.md](03-architecture-patterns.md) (incluye 06 y 07) |
| 04 | `coding-guidelines.md` | ✅ [04-coding-guidelines.md](04-coding-guidelines.md) |
| 05 | `ui-patterns.md` | ⛔ No aplica: sin interfaz propia (Swagger UI es generado) |
| 06 | `data-flow.md` | ↪ Fusionado en 03; los diagramas están en `.sdap/INTERFACE_FLOW.md` |
| 07 | `services-layer.md` | ↪ Fusionado en 03 (handlers = capa de servicios) |
| 08 | `database-orm.md` | ✅ [08-database-orm.md](08-database-orm.md) |
| 09 | `error-handling.md` | ✅ [09-error-handling.md](09-error-handling.md) |
| 10 | `auth-security.md` | ✅ [10-auth-security.md](10-auth-security.md) |
| 11 | `testing-strategy.md` | ✅ [11-testing-strategy.md](11-testing-strategy.md) |
| 12 | `dependencies.md` | ↪ Fusionado en 01 |
| 13 | `how-to-add-features.md` | ✅ [13-how-to-add-features.md](13-how-to-add-features.md) |
| 14 | `ai-rules.md` | ✅ [14-ai-rules.md](14-ai-rules.md) |

**Evidencia del proceso:** [audits/](audits/) (informes del auditor por tarea).

> **Estado actual:** documentación *descriptiva* del código implementado (T-00 a T-26). Cada tarea convirtió estas guías en descripción del código real mediante Reverse Sync; el punto de entrada para personas es el [README](../../README.md) de la raíz.
