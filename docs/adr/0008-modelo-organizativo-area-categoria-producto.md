# ADR-0008: Modelo organizativo Área → Categoría → Producto

- **Estado:** Aceptada
- **Fecha:** 2026-10-22

## Contexto
La visión del sistema es un almacén de gran superficie (referencia: hipermercado), organizado en departamentos generales (Alimentos, Aseo, Herramientas) con supervisores responsables por departamento. La clasificación de productos necesita mayor granularidad (Lácteos, Granos, Bebidas…).

## Decisión
Introducir la entidad **`Area`** (departamento):

```
Area (Alimentos) 1 ──► N Category (Lácteos) 1 ──► N Product
```

- `Area`: `Id`, `Code` (único, p. ej. `FOOD`), `Name`, `IsActive`.
- `Category`: `Id`, `AreaId` (obligatorio), `Name` (único entre activas), `IsActive`.
- `Product`: `Id`, `Sku` (único entre activos), `Name`, `Description`, `CategoryId` (obligatorio, una sola categoría), `Stock`, `MinimumStock`, `IsActive`, campos de desactivación/reactivación, `CreatedAt`, `UpdatedAt`.
- Sin precio: el sistema gestiona inventario, no ventas.

## Alternativas
| Opción | Motivo de descarte |
|---|---|
| Área = categoría general | Pierde la clasificación fina; filtros y stock bajo por categoría pierden utilidad. |
| Categorías jerárquicas (árbol autorreferenciado) | Sin FOREIGN KEYS obliga a validar ciclos y profundidad en la aplicación. |

## Consecuencias
- Cada concepto tiene una responsabilidad: el área es organizativa, la categoría clasifica.
- El enunciado pide *"al menos"* Productos, Categorías y Movimientos; añadir Áreas está permitido y justificado por el dominio.
- Fuera de alcance: **multi-sede** (un supervisor por área *y* sede). Registrado en [IMPROVEMENTS.md](../IMPROVEMENTS.md).
