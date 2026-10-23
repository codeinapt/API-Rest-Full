# ADR-0006: Soft delete, desactivación con motivo y reactivación

- **Estado:** Aceptada
- **Fecha:** 2026-10-22

## Contexto
Sin FOREIGN KEYS, un borrado físico de un producto dejaría movimientos huérfanos y destruiría el histórico de inventario. Además, un producto puede retirarse con stock físico aún en almacén (vencido, dañado, retirado del mercado…).

## Decisión
- **Soft delete** (`IsActive`) en Áreas, Categorías y Productos. Los listados filtran por `status` (por defecto `active`) de forma explícita en cada consulta. No se usa `HasQueryFilter` global, porque el detalle por `id` debe devolver también los inactivos (por ejemplo, para ver un producto con stock congelado).
- **Productos, dos vías de baja:**
  - `DELETE /products/{id}`: baja de catálogo simple. **Solo si `Stock = 0`**; motivo automático `Discontinued`. Con stock > 0 responde `409 Conflict` indicando usar `/deactivation`.
  - `POST /products/{id}/deactivation`: baja con **stock congelado**. Body `{ reason, notes }`.
- **Motivo estructurado:** `DeactivationReason` = `Expired`, `Damaged`, `Discontinued`, `Recalled`, `Other`. `DeactivationNotes` obligatorio si el motivo es `Other`. Se registran `DeactivatedAt` y `DeactivatedBy` (`sub` del JWT).
- **Producto inactivo:** no admite movimientos `In` ni `Out`; **sí admite `Adjustment`** (para registrar merma o destrucción y llevar el stock a 0 con trazabilidad).
- **Reactivación:** `POST /products/{id}/reactivation`, solo supervisor del área (ADR-0009). Se registran `ReactivatedAt` y `ReactivatedBy`.
- **Categorías y Áreas:** `DELETE` sin motivo. No se puede desactivar una categoría con productos activos, ni un área con categorías activas (`409`).

## Alternativas
- **Borrado físico:** pierde el histórico.
- **`DELETE` con body:** semántica no definida en RFC 9110; algunos proxies descartan el body.
- **Motivo por query string:** las notas acabarían en logs y con límite de longitud.

## Consecuencias
- **Limitación conocida:** una nueva desactivación sobrescribe los datos de la anterior. Registrada en [IMPROVEMENTS.md](../IMPROVEMENTS.md); el libro de movimientos ya audita lo crítico (el stock).
