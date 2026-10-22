# ADR-0011: Consultas: paginación, filtros y punto de reorden

- **Estado:** Aceptada
- **Fecha:** 2026-10-22

## Contexto
Un almacén grande tiene miles de productos. Un listado sin límites es un riesgo de rendimiento y disponibilidad.

## Decisión
- **Paginación obligatoria** por offset: `page`, `pageSize` (por defecto 20, máximo 100). Respuesta: `{ items, page, pageSize, totalCount, totalPages }`.
- **Filtros de productos:**
  - `categoryId`;
  - `status=active|inactive|all` (por defecto `active`);
  - `search` (nombre o SKU);
  - `lowStock=true`;
  - `sortBy=name|sku|stock` + `sortDirection`, con lista blanca de campos permitidos.
- **Punto de reorden:** campo `MinimumStock` en el producto. `lowStock=true` devuelve `Stock <= MinimumStock`.
- **Consulta de movimientos:** `GET /products/{id}/movements` con filtros `type`, `from`, `to`; paginado y ordenado por `CreatedAt` descendente.
- Lecturas EF con `AsNoTracking()` y proyección a DTO.
- **Índices:** `(CategoryId, IsActive)`, `Sku` (único filtrado), `(ProductId, CreatedAt)` en movimientos.

## Alternativas
- **Umbral de stock bajo por parámetro (`?maxStock=10`):** no tiene sentido de negocio (10 tornillos ≠ 10 neveras).
- **Paginación keyset (por cursor):** mejor con grandes volúmenes; mejora futura.
- **OData / GraphQL:** sobreingeniería para el alcance.

## Consecuencias
- El enunciado solo exige registrar movimientos; se añade su consulta porque un histórico no consultable no aporta valor.
