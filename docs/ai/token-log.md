# Registro de consumo de tokens

Evidencia de la gestión de tokens (métrica **TEI** de SDAP: tokens por línea útil). Una fila por tarea, con los datos que reporta la herramienta al cerrar la sesión de la tarea.

| Tarea | Fecha | Modelo | Tokens entrada | Tokens salida | Coste (USD) | Líneas netas (+/−) | Iteraciones hasta PASS | Notas |
|---|---|---|---|---|---|---|---|---|
| Inception + Capa 0 + Capa 1 | 2026-10-22 | Claude Opus | — | — | — | — | — | Fase de diseño en una sola sesión de diálogo; sin código |

**Estrategias aplicadas:**
- tareas atómicas con un payload mínimo (`13-how-to-add-features.md` §1);
- `/clear` entre tareas;
- auditor en un subagente con contexto aislado;
- modelo según la complejidad;
- memoria persistente entre sesiones;
- documentación de librerías bajo demanda.
