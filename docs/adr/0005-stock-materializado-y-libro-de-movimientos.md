# ADR-0005: Stock materializado + libro de movimientos inmutable

- **Estado:** Aceptada
- **Fecha:** 2026-10-22

## Contexto
Hay que registrar entradas y salidas de inventario y conocer el stock actual de cada producto.

## Decisión
- Los **movimientos** son un libro contable de solo inserción.
- `Product.Stock` es el **saldo materializado**, actualizado en la **misma transacción Dapper** que inserta el movimiento:

```sql
UPDATE Products SET Stock = Stock + @Delta
WHERE Id = @ProductId AND Stock + @Delta >= 0;
-- filas afectadas = 0 → producto inexistente o stock insuficiente → rollback
INSERT INTO InventoryMovements (...) VALUES (...);
```

## Alternativas
- **Calcular el stock sumando movimientos:** siempre exacto, pero las lecturas son costosas y evitar stock negativo con concurrencia exige aislamiento `SERIALIZABLE` o bloqueos.

## Consecuencias
- **Concurrencia segura sin bloqueos explícitos:** la condición en el `UPDATE` es atómica.
- **Sustituye a la FK:** el `WHERE Id = @ProductId` valida la existencia del producto.
- **Lecturas baratas:** una columna, no una agregación.
- **Auditable:** la suma de movimientos debe coincidir con `Stock` (invariante verificable). Cada movimiento guarda `PreviousStock` y `ResultingStock`.
