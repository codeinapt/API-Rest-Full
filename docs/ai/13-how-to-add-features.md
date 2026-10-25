# 13 · Cómo ejecutar una tarea (algoritmo para el agente)

## 1. Protocolo de inyección de contexto (gestión de tokens)
Para cada tarea se carga **solo** este payload, no el repositorio entero:

| Bloque | Contenido | Origen |
|---|---|---|
| `governance_rules` | Las reglas, los DTOs y las tablas que la tarea cita | Extractos de `.sdap/DOMAIN_LOGIC.md` y `.sdap/DATA_MINDMAP.md` |
| `functional_boundary` | El caso de uso de la tarea | `.sdap/USE_CASES.md` |
| `api_boundary` | Las filas de rutas de la tarea | `.sdap/SCREEN_FLOW.md` §3 |
| `guardrails` | Reglas y prohibiciones | `AGENTS.md`, `docs/ai/14-ai-rules.md` |
| `execution_boundary` | El diagrama de secuencia y la fila de alcance del módulo | `.sdap/INTERFACE_FLOW.md` |
| `local_context` | Solo los archivos de código que la tarea toca o de los que depende | Búsqueda por símbolo, no lectura masiva |
| `task_instruction` | La entrada de la tarea | `.sdap/TASKS.md` |

## 2. Plantilla del prompt de ejecución (SDAP Prompt)

```
Execute task T-XX from .sdap/TASKS.md under SDAP.
Load only: <files and sections listed in the task's "Payload">.
Constraints: AGENTS.md rules and guardrails; scope limited to the task's "Scope".
Work TDD: failing tests first (commit test(T-XX)), then implementation (commit feat(T-XX)).
Stop and ask if anything in the spec is ambiguous or missing.
When green: run the governance-auditor on the task range, save the report in docs/ai/audits/T-XX.md, fix FAILs, do the Reverse Sync of docs/ai/ and mark the task done.
```

## 3. Algoritmo
1. Leer la tarea. Si depende de otra no completada, detenerse.
2. Cargar el payload (§1).
3. **Tests primero** según `11-testing-strategy.md`.
4. Implementar siguiendo `02-folder-structure.md` y `03-architecture-patterns.md`.
5. `dotnet build` (0 warnings) y `dotnet test` (verde).
6. Auditoría (`governance-auditor`) → `docs/ai/audits/T-XX.md`. Corregir hasta tener PASS.
7. **Reverse Sync:** si el cambio afectó a stack, carpetas, patrones, errores, seguridad o pruebas, actualizar el archivo correspondiente de `docs/ai/`.
8. Marcar `[x]` en `TASKS.md`.

## 4. Tipos de trabajo fuera de `TASKS.md`
| Tipo | Procedimiento |
|---|---|
| **Bug** | ¿El código viola `.sdap/`? Se corrige el código con un test que reproduzca el fallo. ¿Revela un caso no previsto? Primero se para y el humano actualiza `.sdap/` (y un ADR si es una decisión). |
| **Refactor** | `DATA_MINDMAP` (tablas y DTOs) y las firmas públicas quedan congeladas. Solo cambia la estructura interna. Los tests existentes no se modifican. |
| **Feature nueva** | Vuelve a la fase de Inception: ADR → actualización de `.sdap/` → tarea nueva en `TASKS.md`. |
