# ADR-0013: Flujo de trabajo con IA

- **Estado:** Aceptada
- **Fecha:** 2026-10-22

## Contexto
El enunciado valora orquestar la IA (no solo consumirla): instrucciones explícitas, rol de revisión, rol de verificación, flujo documentado y gestión eficiente de tokens. El candidato es responsable del 100 % del código.

## Decisión
Flujo en 8 fases, donde cada fase deja evidencia en el repositorio:

| Fase | Actividad | Evidencia |
|---|---|---|
| 1. Inception | Discusión humano–IA, sin código | `docs/adr/` |
| 2. Especificación | Congelamiento del resultado | `.sdap/` (Capa 0) |
| 3. Instrucciones | Stack, arquitectura, 7 reglas de Clean Code, comandos, prohibiciones | `AGENTS.md` (estándar multi-herramienta); `CLAUDE.md` lo importa |
| 4. Tareas | División en tareas atómicas (caso de uso + secuencia + reglas) | `.sdap/TASKS.md` |
| 5. Implementación | Claude Code ejecuta una tarea por sesión con el prompt SDAP | Commits `test:` / `feat:` |
| 6. Revisión | Subagente auditor de solo lectura revisa el `git diff` contra las reglas y la Tech Fence | `docs/ai/audits/TASK-XX.md` (PASS/FAIL) |
| 7. Verificación | Build, tests y cobertura; analizadores para las reglas comprobables automáticamente | GitHub Actions + badge |
| 8. Declaración | Herramientas, fases, validación y correcciones manuales | Sección "Uso de IA" del README |

**Gestión de tokens:**
- tareas atómicas y limpieza de contexto entre tareas;
- auditor en subagente con contexto aislado;
- modelo según la complejidad (más potente para diseño, intermedio para implementación);
- memoria persistente entre sesiones;
- Context7 para documentación bajo demanda.

> **Enmienda (2026-10-25):** se descarta el registro del consumo de tokens por tarea (métrica TEI de SDAP). Solo medía el gasto y no aportaba al proceso. Las estrategias de gestión de tokens de esta lista se mantienen.

## Consecuencias
- El proceso es reproducible con cualquier herramienta que soporte `AGENTS.md`.
- La revisión humana sigue siendo obligatoria: el auditor no sustituye la responsabilidad del candidato.
