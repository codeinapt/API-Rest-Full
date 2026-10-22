# ADR-0010: Errores con ProblemDetails (RFC 9457) y mensajes en español

- **Estado:** Aceptada
- **Fecha:** 2026-10-22

## Contexto
Se necesita un formato de error uniforme. La regla 1 de Clean Code exige código 100 % en inglés, salvo los mensajes que se muestran en las respuestas.

## Decisión
- Todas las respuestas de error usan **ProblemDetails (RFC 9457)**, estándar soportado nativamente por ASP.NET Core.
- `title` y `detail` en **español**.
- Extensión `code` en inglés, estable y legible por máquina, para que los clientes reaccionen sin depender del texto:

```json
{
  "type": "https://httpstatuses.io/409",
  "title": "Conflicto de inventario",
  "status": 409,
  "detail": "Stock insuficiente: disponible 3, solicitado 5.",
  "code": "INSUFFICIENT_STOCK"
}
```

- Los textos en español se centralizan en una clase (`ErrorMessages.InsufficientStock = "..."`): identificador en inglés, valor en español. No hay español disperso en el código.
- Los mensajes de validación también van en español.

## Consecuencias
- Contrato de errores único y documentable en Swagger.
- Un mapeador central de excepciones de dominio → ProblemDetails, cubierto con pruebas unitarias.
