# ADR-0015: Interpretación operativa de las reglas de Clean Code

- **Estado:** Aceptada
- **Fecha:** 2026-10-23

## Contexto
La prueba se evalúa con las reglas de Clean Code de Redarbor. El enunciado anuncia **8 reglas, pero enumera 7**. Algunas admiten más de una lectura, y la IA necesita definiciones verificables (no ambiguas) para aplicarlas y auditarlas.

## Decisión
- Se aplican las **7 reglas enumeradas**. La discrepancia se declara en el README.
- **Regla 2 (líneas en blanco):**
  - **Dentro** de un método o bloque: ninguna línea en blanco, salvo exactamente una justo antes del `return` (y no si el `return` es la primera sentencia del bloque).
  - **Entre** miembros de un tipo (métodos, propiedades, constructores): se permite una línea en blanco.
- **Regla 4:** "más de 25 líneas" se mide sobre el cuerpo del método; "anidados más de 1 vez" significa un máximo de un nivel de bucle o condicional dentro de otro.
- **Regla 6:** "una clase por archivo" se extiende a `record` e `interface`, y se exige que el nombre del archivo coincida con el tipo (StyleCop SA1402 y SA1649).
- La definición operativa completa está en `AGENTS.md`, y la comprueba el `governance-auditor`.

## Justificación (regla 2)
La regla habla de espacios **entre líneas de código**. La separación entre miembros no está entre líneas de código, sino entre declaraciones. Además, separar los miembros es la convención de C# y la exige StyleCop por defecto (SA1516). Aplicar la lectura literal también entre miembros produciría archivos difíciles de leer, que es lo contrario del objetivo de Clean Code.

## Consecuencias
- Si Redarbor aplica la lectura estricta, el cambio es mecánico (formateador) y no afecta al diseño.
