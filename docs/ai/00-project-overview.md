# 00 · Visión general del proyecto

## Qué es
API REST de inventario para un almacén de gran superficie (un solo almacén). La estructura es **Área → Categoría → Producto**. Las entradas, salidas y ajustes se registran en un libro de movimientos inmutable, y el stock se mantiene materializado en el producto.

## Para quién
| Rol | Qué hace |
|---|---|
| `operator` | Mantiene productos y registra entradas y salidas |
| `supervisor` | Lo del operario + ajustes y reactivaciones en **sus áreas** |
| `admin` | Gestiona áreas, categorías y la categoría de cada producto. No toca el stock |

## Piezas
- **Inventory.Api:** controllers, seguridad, Swagger.
- **Keycloak:** emisión de tokens OAuth2.
- **SQL Server:** persistencia sin FOREIGN KEYS.
- Todo se levanta con `docker compose up --build`.

## Dónde mirar
| Necesito… | Archivo |
|---|---|
| Reglas de negocio y códigos de error | `.sdap/DOMAIN_LOGIC.md` |
| Tablas y DTOs | `.sdap/DATA_MINDMAP.md` |
| Qué hace cada endpoint | `.sdap/USE_CASES.md` + `.sdap/SCREEN_FLOW.md` |
| Orden de llamadas de una operación | `.sdap/INTERFACE_FLOW.md` |
| Por qué se decidió algo | `docs/adr/` |
| Qué tarea toca ahora | `.sdap/TASKS.md` |
