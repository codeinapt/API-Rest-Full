# 04 · Guía de código

## Las 7 reglas de Clean Code de Redarbor
La definición operativa está en `AGENTS.md`. Aquí van ejemplos.

### Regla 2: líneas en blanco
```csharp
// ✅ Correcto
public ProductResponse Handle(GetProductQuery query)
{
    var product = repository.GetById(query.ProductId);
    EnsureExists(product, query.ProductId);

    return product.ToResponse();
}

// ❌ Incorrecto: línea en blanco entre sentencias
public ProductResponse Handle(GetProductQuery query)
{
    var product = repository.GetById(query.ProductId);

    EnsureExists(product, query.ProductId);
    return product.ToResponse();
}
```

### Regla 4: un solo nivel de anidamiento y máximo 25 líneas
```csharp
// ❌ Dos niveles de anidamiento
foreach (var item in items)
{
    if (item.IsActive)
    {
        if (item.Stock <= item.MinimumStock) { ... }
    }
}

// ✅ Cláusulas de guarda y extracción de métodos
foreach (var item in items.Where(IsLowStockActiveItem))
{
    Notify(item);
}
```

### Regla 7: objetos, no propiedades
```csharp
// ✅
writeRepository.Insert(movement);
// ❌
writeRepository.Insert(productId, type, delta, notes, userId);
```

## Convenciones de C#
| Elemento | Convención | Ejemplo |
|---|---|---|
| Tipos, métodos, propiedades | PascalCase | `RegisterMovementHandler` |
| Parámetros y variables locales | camelCase | `productId` |
| Campos privados | `camelCase` sin guion bajo, `readonly` | `private readonly IClock clock;` |
| Interfaces | Prefijo `I` | `ICurrentUser` |
| Asíncronos | Sufijo `Async` y `CancellationToken` como último parámetro | `GetByIdAsync(id, cancellationToken)` |
| DTOs | `record` inmutable | `record CreateAreaRequest(string Code, string Name)` |
| Constructores | *Primary constructors* (C# 12) en handlers y repositorios | `class XHandler(IRepo repo) : ...` |

- `var` cuando el tipo es evidente.
- `file-scoped namespaces`.
- `sealed` por defecto en clases que no se heredan.
- Nada de `async void`. Nada de `.Result` ni `.Wait()`.
- Las cadenas mostradas al cliente salen de `ErrorMessages` (regla 1).
- Todo lo que no es texto al cliente va en inglés, **incluidos los nombres de tests**.

## Formato
`.editorconfig` en la raíz y StyleCop en el build. Las reglas de StyleCop que contradicen las 7 reglas de Redarbor se desactivan en `.editorconfig`, justificándolo en un comentario (en inglés).
