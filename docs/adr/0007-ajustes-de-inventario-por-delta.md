# ADR-0007: Ajustes de inventario por delta

- **Estado:** Aceptada
- **Fecha:** 2026-10-22

## Contexto
Además de entradas y salidas, un inventario real necesita correcciones: conteos físicos, mermas, robos, errores de digitación.

## Decisión
`MovementType` = `In` | `Out` | `Adjustment`. El ajuste se expresa como un **delta con signo**.

| # | Regla |
|---|---|
| R1 | `In` / `Out`: `Quantity > 0`. `Adjustment`: delta ≠ 0 (positivo o negativo). |
| R2 | Ningún movimiento puede dejar el stock negativo (garantizado en el `UPDATE` atómico). |
| R3 | `Adjustment` exige `AdjustmentReason`: `PhysicalCountCorrection`, `Damage`, `Expiration`, `LossOrTheft`, `Found`, `DataEntryError`. |
| R4 | Los movimientos son inmutables. Un error se corrige con un ajuste compensatorio que referencia al original (`RelatedMovementId`). |
| R5 | Cada movimiento guarda `PreviousStock` y `ResultingStock`. |
| R6 | Producto inactivo: solo admite `Adjustment`. |
| R7 | Cada movimiento registra `CreatedBy` (`sub` del JWT) y `CreatedAt` (UTC). |
| R8 | Los ajustes solo los realiza un **supervisor del área** del producto (ADR-0009). |

## Alternativas
- **Ajuste por conteo físico** ("conté 42"): más realista, pero si entre el conteo y el registro ocurre una salida, el conteo la sobrescribe. Requeriría control optimista (`ExpectedStock`).

## Consecuencias
- Seguro ante concurrencia y simple.
- El ajuste por conteo físico queda como evolución futura en [IMPROVEMENTS.md](../IMPROVEMENTS.md).
