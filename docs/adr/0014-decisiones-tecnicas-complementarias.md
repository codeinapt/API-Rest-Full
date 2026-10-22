# ADR-0014: Decisiones técnicas complementarias

- **Estado:** Aceptada
- **Fecha:** 2026-10-22

## Contexto
Al cristalizar la Capa 0 surgieron decisiones técnicas que no se habían discutido en la Inception.

## Decisiones

| Tema | Decisión | Alternativa descartada y motivo |
|---|---|---|
| Identificadores | `INT IDENTITY`; `BIGINT IDENTITY` en movimientos (el libro crece sin límite) | `Guid`: más purista, pero incómodo de probar a mano y fragmenta índices si no es secuencial |
| Esquema de BD | **DbUp**: scripts SQL versionados embebidos que la API aplica al arrancar. Un script commiteado nunca se edita | Migraciones EF (EF no debe escribir); contenedor init con `sqlcmd` (al depurar en local habría que ejecutar los scripts a mano) |
| Endpoints de inventario | Separados: `POST /products/{id}/movements` (In/Out, operario) y `POST /products/{id}/adjustments` (supervisor del área). Misma tabla | Un único endpoint: la autorización dependería del body y `quantity` cambiaría de significado según el tipo |
| Estilo de API | ASP.NET Core Controllers | Minimal APIs: menos estructura para "una clase por archivo" y para documentar en Swagger |
| Mapeo | Manual | AutoMapper: licencia comercial desde 2025 y oculta errores de mapeo |
| Códigos HTTP | 400 formato · 404 recurso de la URL · 409 conflicto de estado · 422 referencia inválida en el body | Usar 400 para todo: impide al cliente distinguir el tipo de error |
| Defensa en profundidad | Restricciones `CHECK` en BD (`Stock >= 0`, `ResultingStock = PreviousStock + Delta`) | Solo validación en código: el enunciado prohíbe FKs, no CHECK |

## Consecuencias
- Todas estas decisiones quedan reflejadas en la Capa 0 (`.sdap/`).
