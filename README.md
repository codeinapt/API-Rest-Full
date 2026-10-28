# Inventory API

[![CI](https://github.com/codeinapt/API-Rest-Full/actions/workflows/ci.yml/badge.svg)](https://github.com/codeinapt/API-Rest-Full/actions/workflows/ci.yml)

API REST de gestión de inventario para un almacén organizado en **Áreas → Categorías → Productos**, con stock materializado en el producto y un **libro de movimientos inmutable** (entradas, salidas y ajustes). Prueba técnica de Redarbor, desarrollada con IA siguiendo la metodología **SDAP** (*Spec-Driven Agentic Programming*).

| | |
|---|---|
| Plataforma | .NET 10, ASP.NET Core Controllers (`LangVersion` 12) |
| Arquitectura | Clean Architecture + CQRS con **Mediator** (lecturas con EF Core, escrituras con Dapper) |
| Base de datos | SQL Server 2022, esquema con scripts **DbUp**, sin `FOREIGN KEY` |
| Seguridad | Keycloak 26 (OAuth2/OIDC, Authorization Code + PKCE) y validación de JWT |
| Pruebas | xUnit, NSubstitute, Shouldly, coverlet y Testcontainers (integración) |

Especificación completa: [`.sdap/`](.sdap/) · Decisiones: [`docs/adr/`](docs/adr/README.md) · Contexto del código: [`docs/ai/`](docs/ai/README.md).

---

## 1. Requisitos

| Para… | Necesitas |
|---|---|
| Levantar todo el sistema | Docker con Compose v2 |
| Depurar la API, ejecutar tests y medir la cobertura | .NET SDK 10.0.108 o superior de la banda 10.0.1xx (`global.json`, `rollForward: latestFeature`) |
| Tests de integración | Docker en marcha (Testcontainers arranca SQL Server) |

Puertos del host: `8080` (API y Swagger), `8180` (Keycloak) y `1433` (SQL Server).

## 2. Configuración (`.env`)

Toda la configuración se lee de **variables de entorno**. Ningún secreto vive en el código ni en `appsettings*.json`.

```bash
cp .env.example .env
# Edita las contraseñas de ejemplo de .env (está en .gitignore)
```

| Variable | Uso |
|---|---|
| `MSSQL_SA_PASSWORD` | Contraseña SA de SQL Server (mínimo 8 caracteres con mayúsculas, minúsculas, dígitos y símbolos) |
| `KC_BOOTSTRAP_ADMIN_USERNAME` / `KC_BOOTSTRAP_ADMIN_PASSWORD` | Administrador de la consola de Keycloak |
| `AUTH_AUDIENCE` | Audiencia esperada en el token (`inventory-api`) |
| `SWAGGER_OAUTH_CLIENT_ID` | Cliente público de Swagger UI (`inventory-swagger`) |
| `INVENTORY_DB_CONNECTION` | Cadena de conexión. **Solo para `dotnet run`**: Compose construye la suya |
| `AUTH_AUTHORITY` | Issuer externo del realm, el que ve el navegador. **Solo para `dotnet run`** |
| `AUTH_METADATA_ADDRESS` | Metadatos OIDC. **Solo para `dotnet run`**: dentro de Compose se usan por la red interna (`http://keycloak:8080`) |

Si la contraseña SA cambia, actualiza también `INVENTORY_DB_CONNECTION`. Si falta una variable obligatoria, la API no arranca y nombra la variable que falta.

## 3. Ejecución con Docker

```bash
docker compose up --build
```

Arranca `sqlserver`, `keycloak` (importa el realm de [`keycloak/realm-inventory.json`](keycloak/realm-inventory.json)) y `api`. La API espera a que los otros dos servicios estén *healthy* y, al iniciarse, aplica los scripts DbUp y el *seed*.

| Recurso | URL |
|---|---|
| Swagger UI | http://localhost:8080/swagger |
| Salud | http://localhost:8080/health |
| Consola de Keycloak | http://localhost:8180 (credenciales `KC_BOOTSTRAP_ADMIN_*`) |

**Datos de ejemplo:** el *seed* crea 3 áreas (`FOOD`, `CLEAN`, `TOOLS`), 6 categorías y 12 productos con su historial de entradas. Entre ellos hay uno sin stock (`FOOD-OIL-001`) y tres por debajo del mínimo (`FOOD-YOG-001`, `CLEAN-BLE-001`, `TOOLS-SCR-001`). Solo se aplica sobre una base vacía. Si el volumen se creó antes del *seed*, recréalo con `docker compose down -v`. Hacer lo mismo tras cambiar el realm obliga a Keycloak a reimportarlo.

## 4. Depuración local

La API se ejecuta fuera de Docker, con SQL Server y Keycloak en contenedores:

```bash
docker compose up -d sqlserver keycloak       # si el servicio api está levantado: docker compose stop api
while IFS= read -r line; do case "$line" in [A-Z]*=*) export "$line";; esac; done < .env
export ASPNETCORE_ENVIRONMENT=Development     # permite metadatos OIDC por HTTP
dotnet run --project src/Inventory.Api --urls http://localhost:8080
```

- La primera línea exporta cada `CLAVE=valor` de `.env` tal cual. No vale `source .env`, porque la cadena de conexión contiene `;` y espacios sin comillas.
- El puerto **8080** es obligatorio: es la única *redirect URI* registrada para Swagger en Keycloak.
- Para depurar desde el IDE, define esas mismas variables en el perfil de ejecución de `Inventory.Api`. No hay `launchSettings.json` versionado, para no publicar valores locales.

## 5. Usuarios de prueba

Contraseña común: **`Inventory123!`** (solo desarrollo).

| Usuario | Rol | Áreas que supervisa |
|---|---|---|
| `operator1` | `operator` | — |
| `supervisor.food` | `supervisor` | `FOOD` |
| `supervisor.tools` | `supervisor` | `TOOLS` |
| `admin1` | `admin` | — |

El área de un supervisor sale de su grupo de Keycloak (`/areas/FOOD`…) y llega en la claim `areas` del token.

## 6. Probar en Swagger

1. Abre http://localhost:8080/swagger y pulsa **Authorize**.
2. Marca los scopes `openid`, `inventory.read` e `inventory.write` y pulsa **Authorize**. Swagger usa Authorization Code + PKCE, así que no hay *client secret* que rellenar.
3. Inicia sesión en Keycloak con uno de los usuarios. Para **cambiar de rol**, pulsa **Logout** en el diálogo de Swagger, cierra la sesión de Keycloak (http://localhost:8180/realms/inventory/account o una ventana privada) y vuelve a autorizar.

> Sin `inventory.write`, cualquier escritura devuelve 403 aunque el rol lo permita. Sin token, todo menos `/health` devuelve 401.

Los `id` de los productos se obtienen con `GET /api/v1/products?search=FOOD-MILK`.

### Matriz de permisos

| Operación | `operator` | `supervisor` | `admin` |
|---|:-:|:-:|:-:|
| Consultar todo | ✅ | ✅ | ✅ |
| Crear / editar / desactivar áreas y categorías | ❌ | ❌ | ✅ |
| Crear / editar producto | ✅ | ✅ | ❌ |
| Cambiar categoría de producto | ❌ | ❌ | ✅ |
| `DELETE` producto (stock 0) / desactivar con motivo | ✅ | ✅ | ❌ |
| Registrar entrada / salida | ✅ | ✅ | ❌ |
| Registrar ajuste / reactivar producto | ❌ | ✅ *solo sus áreas* | ❌ |

El `admin` no toca el stock (separación de funciones, ADR-0009).

### Recorrido por rol

| Usuario | Petición | Resultado esperado |
|---|---|---|
| `admin1` | `POST /api/v1/areas` `{"code":"TOYS","name":"Juguetes"}` | **201** con `Location` |
| `admin1` | `POST /api/v1/products/{id}/movements` | **403** `FORBIDDEN`: el admin no opera stock |
| `operator1` | `POST /api/v1/products/{id}/movements` `{"type":"Out","quantity":1}` sobre `FOOD-MILK-001` | **201**, stock 50 → 49 |
| `operator1` | Misma salida con `"quantity": 1000` | **409** `INSUFFICIENT_STOCK` |
| `operator1` | `POST /api/v1/areas` | **403** `FORBIDDEN`: solo el admin gestiona el catálogo organizativo |
| `operator1` | `POST /api/v1/products/{id}/adjustments` | **403** `FORBIDDEN`: ajustar exige el rol supervisor |
| `supervisor.food` | `POST /api/v1/products/{id}/adjustments` `{"delta":-2,"reason":"Damage"}` sobre `FOOD-MILK-001` | **201** |
| `supervisor.food` | El mismo ajuste sobre `TOOLS-HAM-001` | **403** `AREA_ACCESS_DENIED`: área ajena |
| `supervisor.tools` | El mismo ajuste sobre `TOOLS-HAM-001` | **201** |
| `supervisor.tools` | `POST /api/v1/products/{id}/reactivation` sobre un producto de `FOOD` | **403** `AREA_ACCESS_DENIED`: se comprueba el área antes que el estado |

Reactivar un producto de su área exige desactivarlo antes con `POST /api/v1/products/{id}/deactivation`. Cualquier usuario con rol `operator` o `supervisor` puede hacerlo.

Los errores siguen RFC 9457 (`ProblemDetails`), con un `code` estable y el mensaje en español ([ADR-0010](docs/adr/0010-errores-problemdetails-en-espanol.md)). El catálogo completo de rutas está en [`.sdap/SCREEN_FLOW.md`](.sdap/SCREEN_FLOW.md) §3.

## 7. Tests y cobertura

```bash
dotnet build                                   # 0 warnings: los warnings son errores
dotnet test                                    # 556 tests: Domain 143, Application 281, Api 112, integración 20
dotnet test --collect:"XPlat Code Coverage" --results-directory TestResults   # un coverage.cobertura.xml por proyecto
```

- `Inventory.IntegrationTests` necesita Docker: levanta un SQL Server real con Testcontainers y prueba el SQL de Dapper, la concurrencia de stock (RM-03), la unicidad del SKU y el invariante `Stock = Σ Delta`. Para omitirlos: `dotnet test --filter "FullyQualifiedName!~IntegrationTests"`.
- Para un informe HTML local, con la herramienta ReportGenerator instalada:
  ```bash
  reportgenerator -reports:"TestResults/**/coverage.cobertura.xml" -targetdir:coverage -reporttypes:Html
  ```
- **CI** ([`.github/workflows/ci.yml`](.github/workflows/ci.yml)): en cada *push* y PR compila, ejecuta todos los tests y publica el informe HTML como artefacto `coverage-report`. **Falla si la cobertura de líneas de Domain + Application baja del 80 %** (actualmente 96,7 %).

Estrategia completa: [ADR-0012](docs/adr/0012-estrategia-de-pruebas.md) y [`docs/ai/11-testing-strategy.md`](docs/ai/11-testing-strategy.md).

## 8. Reglas de Clean Code

> El enunciado anuncia **8 reglas**, pero enumera **7**. Se aplican esas 7, con la definición operativa de [ADR-0015](docs/adr/0015-interpretacion-reglas-clean-code.md):

1. Todo el código en inglés. Solo los mensajes que recibe el cliente están en español, centralizados en `ErrorMessages.cs`.
2. Sin líneas en blanco dentro de los bloques, salvo una justo antes de un `return`.
3. Nombres autodescriptivos, sin abreviaturas.
4. Una responsabilidad por método: máximo 25 líneas y un solo nivel de anidamiento.
5. Configuración por variables de entorno.
6. Un tipo por archivo, con el mismo nombre (StyleCop SA1402/SA1649 como error).
7. Pasar objetos, no listas de propiedades.

## 9. Uso de IA

El proyecto se ha construido con **Claude Code** (extensión de VS Code) bajo la metodología **SDAP**. La IA no programa a ciegas: trabaja contra una especificación congelada, con una tarea por sesión, y otro agente audita cada tarea. El autor revisa y aprueba cada tarea antes de integrarla y responde del 100 % del código ([ADR-0013](docs/adr/0013-flujo-de-trabajo-con-ia.md)).

### Fases

| Fase | Qué se hizo | Evidencia |
|---|---|---|
| 1. Inception | Discusión humano–IA de cada decisión y sus alternativas, sin código | [`docs/adr/`](docs/adr/README.md) (ADR-0000 a 0015) |
| 2. Especificación | Arquitectura, Tech Fence, reglas de dominio, contratos de datos, casos de uso, rutas y secuencias, congelados como Capa 0 inmutable | [`.sdap/`](.sdap/) |
| 3. Instrucciones | Stack, arquitectura, las 7 reglas, comandos y prohibiciones para cualquier agente | [`AGENTS.md`](AGENTS.md), [`CLAUDE.md`](CLAUDE.md), [`docs/ai/14-ai-rules.md`](docs/ai/14-ai-rules.md) |
| 4. Tareas | 27 tareas atómicas (T-00 a T-26), cada una con su payload de contexto y su alcance de archivos | [`.sdap/TASKS.md`](.sdap/TASKS.md) |
| 5. Implementación | Una tarea por sesión y rama (`feature/T-XX-…` sobre `develop`). TDD: commit `test(T-XX)` en rojo, luego `feat(T-XX)` | Historial de git |
| 6. Revisión | El subagente `governance-auditor` (solo lectura, contexto aislado) revisa el diff contra las reglas, la Tech Fence y el alcance | [`docs/ai/audits/`](docs/ai/audits/README.md) (un informe por tarea) |
| 7. Verificación | Build sin warnings, analizadores StyleCop, tests y umbral de cobertura en CI | [`.github/workflows/ci.yml`](.github/workflows/ci.yml) |
| 8. Sincronización | Tras cada tarea se actualiza la documentación viva del código (*Reverse Sync*) | [`docs/ai/`](docs/ai/README.md) |

### Gestión del contexto

- Cada tarea carga solo su payload, no el repositorio entero ([`docs/ai/13-how-to-add-features.md`](docs/ai/13-how-to-add-features.md)).
- El contexto se limpia entre tareas, y el auditor corre en un subagente con contexto propio.
- El modelo se elige por complejidad: el más capaz para diseño, concurrencia y seguridad, y uno intermedio para la implementación rutinaria y la auditoría.
- La documentación de librerías se consulta bajo demanda, y una memoria persistente conserva las decisiones entre sesiones.

### Validación y correcciones humanas

- **Ambigüedad → parar y preguntar.** Cuando la especificación no fijaba un detalle (orden de listados, mensajes de error nuevos, estrategia de bloqueo, formato del informe de cobertura…), el agente se detuvo y el autor decidió. Las decisiones constan en los commits y en los informes de auditoría.
- **Revisión de decisiones del diseño:** MediatR se sustituyó por Mediator por la licencia comercial de MediatR ≥ 13 ([ADR-0003](docs/adr/0003-clean-architecture-y-cqrs.md)). La métrica de tokens por tarea de SDAP se descartó ([enmienda de ADR-0013](docs/adr/0013-flujo-de-trabajo-con-ia.md)).
- **Desviaciones de alcance:** cuando el auditor marcó un FAIL por tocar archivos fuera del alcance de la tarea, el autor decidió si aceptarlo o corregirlo, y lo dejó escrito. Ejemplo: [T-05](docs/ai/audits/T-05.md).
- **Flujo de git:** T-00 a T-05 se hicieron directamente sobre `main`. Tras detectarlo, el autor impuso `develop` y una rama por tarea, con *merge* tras su aprobación.
- **Verificación manual:** además de los tests, los flujos de autenticación se probaron de extremo a extremo contra el stack de Docker, con tokens reales de Keycloak (Authorization Code + PKCE).

## 10. Limitaciones conocidas

Las mejoras descartadas de forma consciente para esta versión, con su motivo, están en [`docs/IMPROVEMENTS.md`](docs/IMPROVEMENTS.md): historial de estados de producto, multi-sede, ajuste por conteo físico, paginación keyset, reactivación de áreas y categorías, y concurrencia optimista en la edición del catálogo.

Además, la respuesta 201 de `POST /api/v1/products/{id}/movements` devuelve `createdAt` sin el sufijo `Z`. El valor es UTC, pero Dapper lo lee como `DateTime` sin `Kind`. Las consultas de movimientos (`GET`) sí lo devuelven con zona.
