# ADR-0009: Autorización por roles, alcance por área y separación de funciones

- **Estado:** Aceptada
- **Fecha:** 2026-10-22

## Contexto
No basta con saber si un usuario es "supervisor": un supervisor de Alimentos no debe ajustar productos de Herramientas. Además, quien define la estructura del catálogo no debería manipular el inventario.

## Decisión
**Scopes** (qué puede hacer la aplicación cliente): `inventory.read`, `inventory.write`.
**Roles** (qué puede hacer la persona):

| Rol | Alcance | Permisos |
|---|---|---|
| `operator` | Global | Crear/editar productos (sin cambiar categoría), movimientos `In`/`Out`, `DELETE` y `/deactivation` de productos |
| `supervisor` | **Por área** | Lo del operario + `Adjustment` y reactivación **solo en sus áreas** |
| `admin` | Global | Gestión de áreas y categorías, cambio de categoría de un producto. **No toca el stock.** |

**Alcance por área (autorización basada en recurso):**
- Keycloak: un grupo por área (`/areas/FOOD`). Un *protocol mapper* emite la claim `areas: ["FOOD", ...]`.
- API: el handler resuelve producto → categoría → área y consulta a `IAuthorizationService` si el usuario es `supervisor` y tiene ese `Code` en su claim. Si no: `403`.

**Separación de funciones:** el `admin` gestiona el catálogo pero no registra movimientos, ajustes ni reactivaciones.

## Alternativas
- **RBAC puro (supervisor global):** más simple, pero no refleja la responsabilidad real por departamento.
- **Admin con todos los permisos:** práctico, pero concentra en un solo rol la definición de estructura y la manipulación de inventario.

## Consecuencias
- El realm exportado incluye usuarios de prueba para cada rol (operario, supervisor de un área, supervisor de otra área, admin) para poder probar los casos `403` desde Swagger.
- El *authorization handler* de área es lógica pura y se cubre con pruebas unitarias.
- Se usan los `Code` de área (no IDs de base de datos) para no acoplar Keycloak a la BD.
