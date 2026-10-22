# ADR-0000: Metodología SDAP y registro de decisiones con ADRs

- **Estado:** Aceptada
- **Fecha:** 2026-10-22

## Contexto
La prueba valora un flujo de trabajo con IA estructurado y reproducible (propuesta → especificación → tareas → implementación → verificación). Usar la IA de forma transaccional ("pregunta → respuesta → fin") produce deriva de contexto, decisiones no trazables y código que no respeta las reglas del proyecto.

## Decisión
Aplicar **SDAP (Spec-Driven Agentic Programming)**:
1. **Inception dialéctica:** humano e IA discuten el problema sin escribir código hasta alcanzar una comprensión profunda. Cada decisión se registra como ADR en `docs/adr/`.
2. **Congelamiento:** el resultado se cristaliza en la Capa 0 (`.sdap/`).
3. **Ejecución atómica:** la IA implementa tareas acotadas con contexto mínimo.
4. **Revisión y verificación:** auditoría de gobernanza y build + tests en cada tarea.

## Alternativas
- **Uso ad-hoc de chat/agente:** rápido, pero sin trazabilidad ni control de deriva.
- **OpenSpec / SDD (usado en Redarbor):** equivalente en espíritu; se eligió SDAP por su separación explícita entre exploración y ejecución, y por incluir guardrails y agente auditor.

## Consecuencias
- Más tiempo invertido antes de escribir código, a cambio de menos retrabajo.
- Cada decisión queda justificada y es defendible en la entrevista.
