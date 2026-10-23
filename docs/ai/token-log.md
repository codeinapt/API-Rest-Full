# Registro de consumo de tokens

Evidencia de la gestión de tokens (métrica **TEI** de SDAP: tokens por línea útil). Una fila por tarea, con los datos que reporta la herramienta al cerrar la sesión de la tarea.

| Tarea | Fecha | Modelo | Tokens entrada | Tokens salida | Coste (USD) | Líneas netas (+/−) | Iteraciones hasta PASS | Notas |
|---|---|---|---|---|---|---|---|---|
| Inception + Capa 0 + Capa 1 | 2026-10-22 | Claude Opus | — | — | — | — | — | Fase de diseño en una sola sesión de diálogo; sin código |
| T-00 | 2026-10-23 | Claude Opus 5.5 (auditor: Sonnet) | _pendiente `/cost`_ | _pendiente `/cost`_ | _pendiente `/cost`_ | +200 / −0 (aprox.) | 1 | PASS con 2 WARN de alcance; 3 incidencias de versiones resueltas empíricamente |

**Estrategias aplicadas:**
- tareas atómicas con un payload mínimo (`13-how-to-add-features.md` §1);
- `/clear` entre tareas;
- auditor en un subagente con contexto aislado;
- modelo según la complejidad;
- memoria persistente entre sesiones;
- documentación de librerías bajo demanda.
