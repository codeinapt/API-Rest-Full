# TASKS — Plan de ejecución atómica

> Una tarea = una sesión = un conjunto pequeño de commits (`test(T-XX)` → `feat(T-XX)` → `refactor(T-XX)`).
> Procedimiento por tarea: `docs/ai/13-how-to-add-features.md`. Una tarea no empieza hasta que sus dependencias están completas.
> **Payload** = lo único que el agente carga. **Scope** = lo único que el agente puede tocar.

## Leyenda
- `[ ]` pendiente · `[x]` completada (con informe PASS en `docs/ai/audits/`).
- Abreviaturas de rutas: `D/` = `src/Inventory.Domain`, `A/` = `src/Inventory.Application`, `I/` = `src/Inventory.Infrastructure`, `W/` = `src/Inventory.Api`.

---

## Fase 0 — Cimientos

### [x] T-00 · Solución y configuración común
- **Objetivo:**
  - Solución `Inventory.sln` con los 4 proyectos y los 3 proyectos de test unitario, con sus referencias según la regla de dependencias.
  - `Directory.Build.props` (net10.0, LangVersion 12, Nullable, TreatWarningsAsErrors, StyleCop).
  - `Directory.Packages.props` con las versiones estables actuales de la Tech Fence.
  - `global.json`.
  - `.editorconfig` alineado con las 7 reglas.
- **Payload:** `.sdap/ARCH_SKELETON.md` §4–§6, `docs/ai/01`, `docs/ai/02`, `docs/ai/04`.
- **Scope:** raíz (`*.sln`, `Directory.*.props`, `global.json`, `.editorconfig`), `src/*/*.csproj`, `tests/*/*.csproj`.
- **Hecho cuando:** `dotnet build` sin warnings y `dotnet test` ejecuta 0 tests sin error. Reverse Sync: versiones en `docs/ai/01`.

### [ ] T-01 · Docker y salud
- **Objetivo:**
  - `docker-compose.yml` (sqlserver, keycloak, api) con healthchecks y `depends_on: service_healthy`.
  - `Dockerfile` multi-stage de la API.
  - `.env.example`.
  - Endpoint `GET /health`.
- **Payload:** `.sdap/ARCH_SKELETON.md` §7–§8, `.sdap/INTERFACE_FLOW.md` SEQ-09.
- **Scope:** `docker-compose.yml`, `.env.example`, `W/Dockerfile`, `W/Program.cs`.
- **Hecho cuando:** `docker compose up --build` deja los 3 servicios *healthy*. Reverse Sync: imágenes en `docs/ai/01`.
- **Depende de:** T-00.

### [ ] T-02 · Realm de Keycloak
- **Objetivo:** `keycloak/realm-inventory.json` con clientes, scopes, roles, grupos de área, mappers (audiencia y `areas`) y usuarios de prueba. Montado con `--import-realm`.
- **Payload:** `docs/ai/10` (sección Keycloak), `.sdap/DOMAIN_LOGIC.md` §4.
- **Scope:** `keycloak/`, servicio `keycloak` en `docker-compose.yml`.
- **Hecho cuando:** un token de `supervisor.food` obtenido por PKCE contiene `aud: inventory-api`, `realm_access.roles: [supervisor]`, `areas: [FOOD]` y los scopes.
- **Depende de:** T-01.

### [ ] T-03 · Autenticación, políticas y Swagger
- **Objetivo:**
  - JwtBearer (metadata interna e issuer externo).
  - `KeycloakRolesClaimsTransformation`.
  - Las 4 políticas.
  - `ICurrentUser` / `ClaimsCurrentUser`.
  - Swagger con OAuth2 + PKCE.
- **Payload:** `docs/ai/10`, `.sdap/SCREEN_FLOW.md` §2, `.sdap/INTERFACE_FLOW.md` SEQ-01.
- **Scope:** `W/Authorization/*`, `W/Configuration/*`, `W/Program.cs`, `A/Common/Interfaces/ICurrentUser.cs`, `tests/Inventory.Api.Tests/Authorization/*`.
- **Hecho cuando:** hay tests de la transformación de roles y de `ClaimsCurrentUser`, y el botón *Authorize* de Swagger obtiene un token.
- **Depende de:** T-02.

### [ ] T-04 · Manejo de errores y validación
- **Objetivo:**
  - `ErrorCode`, `DomainException` y las excepciones de aplicación.
  - `ErrorMessages` con **todo** el catálogo de `DOMAIN_LOGIC` §6.
  - `ProblemDetailsMapper`, `GlobalExceptionHandler`, ProblemDetails para 401 y 403.
  - `ValidationBehavior`.
- **Payload:** `.sdap/DOMAIN_LOGIC.md` §6, `docs/ai/09`, `.sdap/INTERFACE_FLOW.md` SEQ-08.
- **Scope:** `D/Common/*`, `A/Common/{Exceptions,Messages,Behaviors}/*`, `W/ErrorHandling/*`, `W/Program.cs`, tests correspondientes.
- **Hecho cuando:** existe un test por cada tipo de excepción → status, `code` y texto.
- **Depende de:** T-00.

### [ ] T-05 · Esquema de base de datos
- **Objetivo:**
  - Scripts DbUp `0001`–`0004` (tablas, `CHECK` e índices filtrados de `DATA_MINDMAP` §2, **sin FK**).
  - `DatabaseMigrator` ejecutado al arrancar.
- **Payload:** `.sdap/DATA_MINDMAP.md` §1–§2, `docs/ai/08` (DbUp).
- **Scope:** `I/Migrations/*`, `I/DependencyInjection.cs`, `W/Program.cs`.
- **Hecho cuando:** al arrancar en Docker se crea el esquema completo y un segundo arranque no reaplica nada.
- **Depende de:** T-01.

### [ ] T-06 · Infraestructura de persistencia base
- **Objetivo:**
  - `InventoryReadDbContext` (NoTracking, configuraciones sin relaciones FK).
  - `SqlConnectionFactory`, `IUnitOfWork` / `UnitOfWork`, `IClock` / `SystemClock`.
  - `PaginationParameters`, `PagedResponse<T>` y su validador.
- **Payload:** `docs/ai/03`, `docs/ai/08`, `.sdap/DATA_MINDMAP.md` §4.2–§4.3.
- **Scope:** `I/Persistence/{Read,Write}/*` (base), `A/Common/{Interfaces,Pagination}/*`, tests de paginación.
- **Depende de:** T-04, T-05.

---

## Fase 1 — Áreas

### [ ] T-07 · Dominio `Area` (TDD)
- **Objetivo:** entidad `Area` con `Create`, `Rename` y `Deactivate`, más las reglas RA-01 a RA-04 en el dominio.
- **Payload:** `.sdap/DOMAIN_LOGIC.md` §3.1 y §5.2, `.sdap/DATA_MINDMAP.md` §3.
- **Scope:** `D/Areas/*`, `tests/Inventory.Domain.Tests/Areas/*`.
- **Depende de:** T-04.

### [ ] T-08 · Commands de Área (CU-01, CU-02, CU-03)
- **Objetivo:** commands, validadores, handlers y `IAreaWriteRepository` + implementación Dapper.
- **Payload:** `.sdap/USE_CASES.md` CU-01–03, `.sdap/INTERFACE_FLOW.md` SEQ-03, DTOs de área.
- **Scope:** `A/Areas/Commands/*`, `A/Areas/Ports/IAreaWriteRepository.cs`, `A/Areas/Dtos/*`, `I/Persistence/Write/AreaWriteRepository.cs`, tests.
- **Depende de:** T-06, T-07.

### [ ] T-09 · Queries de Área (CU-04) y `AreasController` (E01–E05)
- **Objetivo:** queries con EF, `IAreaReadRepository` y el controller con sus políticas y códigos de respuesta documentados en Swagger.
- **Payload:** `.sdap/USE_CASES.md` CU-04, `.sdap/SCREEN_FLOW.md` E01–E05, SEQ-02.
- **Scope:** `A/Areas/Queries/*`, `A/Areas/Ports/IAreaReadRepository.cs`, `I/Persistence/Read/AreaReadRepository.cs`, `W/Controllers/AreasController.cs`, tests.
- **Depende de:** T-03, T-08.

---

## Fase 2 — Categorías

### [ ] T-10 · Dominio `Category` (TDD)
- **Objetivo:** entidad `Category` con las reglas RC-01 a RC-04.
- **Payload:** `.sdap/DOMAIN_LOGIC.md` §3.2.
- **Scope:** `D/Categories/*`, tests.
- **Depende de:** T-04.

### [ ] T-11 · Commands de Categoría (CU-05, CU-06, CU-07)
- **Objetivo:** commands con validación del área padre (`UPDLOCK, HOLDLOCK`) y desactivación condicionada a no tener productos activos.
- **Payload:** `.sdap/USE_CASES.md` CU-05–07, SEQ-03, casos límite de `INTERFACE_FLOW`.
- **Scope:** `A/Categories/Commands/*`, `A/Categories/Ports/ICategoryWriteRepository.cs`, `A/Categories/Dtos/*`, `I/Persistence/Write/CategoryWriteRepository.cs`, tests.
- **Depende de:** T-08, T-10.

### [ ] T-12 · Queries de Categoría (CU-08) y `CategoriesController` (E06–E10)
- **Payload:** `.sdap/USE_CASES.md` CU-08, `.sdap/SCREEN_FLOW.md` E06–E10.
- **Scope:** `A/Categories/Queries/*`, `I/Persistence/Read/CategoryReadRepository.cs`, `W/Controllers/CategoriesController.cs`, tests.
- **Depende de:** T-09, T-11.

---

## Fase 3 — Productos

### [ ] T-13 · Dominio `Product` (TDD)
- **Objetivo:** `Create`, `UpdateDetails`, `ChangeCategory`, `Delete`, `Deactivate`, `Reactivate` e `IsLowStock`, con las reglas RP-01 a RP-14 y la máquina de estados §5.1.
- **Payload:** `.sdap/DOMAIN_LOGIC.md` §2, §3.3 y §5.1.
- **Scope:** `D/Products/*`, tests.
- **Depende de:** T-04.

### [ ] T-14 · Crear y editar producto (CU-09, CU-10)
- **Payload:** `.sdap/USE_CASES.md` CU-09–10, SEQ-03.
- **Scope:** `A/Products/Commands/{CreateProduct,UpdateProduct}/*`, `A/Products/Ports/IProductWriteRepository.cs`, `A/Products/Dtos/*`, `I/Persistence/Write/ProductWriteRepository.cs`, tests.
- **Depende de:** T-11, T-13.

### [ ] T-15 · Cambiar categoría de producto (CU-11)
- **Payload:** `.sdap/USE_CASES.md` CU-11.
- **Scope:** `A/Products/Commands/ChangeProductCategory/*`, `ProductWriteRepository`, tests.
- **Depende de:** T-14.

### [ ] T-16 · Eliminar y desactivar producto (CU-12, CU-13)
- **Payload:** `.sdap/USE_CASES.md` CU-12–13, SEQ-06.
- **Scope:** `A/Products/Commands/{DeleteProduct,DeactivateProduct}/*`, `ProductWriteRepository`, tests.
- **Depende de:** T-14.

### [ ] T-17 · Alcance por área y reactivación (CU-14)
- **Objetivo:** `AreaAccessPolicy` (RS-04), `GetSnapshotWithArea` y reactivación con la traducción de la violación de SKU único.
- **Payload:** `.sdap/USE_CASES.md` CU-14, SEQ-07, `.sdap/DOMAIN_LOGIC.md` RS-04 y RP-11–RP-13, `docs/ai/10`.
- **Scope:** `A/Common/Authorization/AreaAccessPolicy.cs`, `A/Products/Commands/ReactivateProduct/*`, `ProductWriteRepository`, tests.
- **Depende de:** T-03, T-16.

### [ ] T-18 · Queries de Producto (CU-15) y `ProductsController` (E11–E18)
- **Objetivo:** listado con filtros (`categoryId`, `status`, `search`, `lowStock`), orden con lista blanca, detalle y el controller completo.
- **Payload:** `.sdap/USE_CASES.md` CU-15, `.sdap/SCREEN_FLOW.md` E11–E18, `.sdap/DATA_MINDMAP.md` §4.3, `docs/ai/08` (EF).
- **Scope:** `A/Products/Queries/*`, `I/Persistence/Read/ProductReadRepository.cs`, `W/Controllers/ProductsController.cs`, tests.
- **Depende de:** T-12, T-15, T-16, T-17.

---

## Fase 4 — Movimientos

### [ ] T-19 · Dominio `InventoryMovement` (TDD)
- **Objetivo:** fábricas `CreateInbound`, `CreateOutbound` y `CreateAdjustment`, con las reglas RM-01 a RM-10.
- **Payload:** `.sdap/DOMAIN_LOGIC.md` §2, §3.4 y §5.3.
- **Scope:** `D/Movements/*`, tests.
- **Depende de:** T-13.

### [ ] T-20 · Registrar entrada / salida (CU-16)
- **Objetivo:** `ApplyDelta` atómico con `OUTPUT`, inserción del movimiento y transacción.
- **Payload:** `.sdap/USE_CASES.md` CU-16, SEQ-04, `docs/ai/08` (Dapper).
- **Scope:** `A/Movements/Commands/RegisterMovement/*`, `A/Movements/Ports/IMovementWriteRepository.cs`, `I/Persistence/Write/MovementWriteRepository.cs`, `ProductWriteRepository.ApplyDelta`, tests.
- **Depende de:** T-14, T-19.

### [ ] T-21 · Registrar ajuste (CU-17)
- **Payload:** `.sdap/USE_CASES.md` CU-17, SEQ-05.
- **Scope:** `A/Movements/Commands/RegisterAdjustment/*`, `MovementWriteRepository`, tests.
- **Depende de:** T-17, T-20.

### [ ] T-22 · Consultar movimientos (CU-18) y controllers de movimientos (E19–E21)
- **Payload:** `.sdap/USE_CASES.md` CU-18, `.sdap/SCREEN_FLOW.md` E19–E21.
- **Scope:** `A/Movements/Queries/*`, `I/Persistence/Read/MovementReadRepository.cs`, `W/Controllers/{ProductMovementsController,ProductAdjustmentsController}.cs`, tests.
- **Depende de:** T-18, T-21.

---

## Fase 5 — Cierre

### [ ] T-23 · Datos semilla
- **Objetivo:** script DbUp con áreas `FOOD`, `CLEAN` y `TOOLS`, categorías y productos, con stock inicial cargado **mediante movimientos `In`** (RM-11).
- **Payload:** `.sdap/DATA_MINDMAP.md` §2.6.
- **Scope:** `I/Migrations/Scripts/NNNN_SeedData.sql`.
- **Depende de:** T-22.

### [ ] T-24 · Pruebas de integración *(extra)*
- **Objetivo:** Testcontainers. Concurrencia de salidas (RM-03), `DELETE` frente a entrada, unicidad filtrada, invariante del libro (RM-11).
- **Payload:** `docs/ai/11`, casos límite de `.sdap/INTERFACE_FLOW.md`.
- **Scope:** `tests/Inventory.IntegrationTests/*`, `Directory.Packages.props` (Testcontainers).
- **Depende de:** T-22.

### [ ] T-25 · Integración continua
- **Objetivo:** GitHub Actions que ejecute build, tests y cobertura en cada push y publique el informe como artefacto.
- **Scope:** `.github/workflows/ci.yml`.
- **Depende de:** T-00.

### [ ] T-26 · README final
- **Objetivo:**
  - Requisitos, configuración (`.env`), `docker compose up`, depuración local, usuarios de prueba, cómo probar en Swagger cada rol (incluidos los 403 por área), cómo ejecutar tests y cobertura.
  - **Sección "Uso de IA":** herramientas, fases, validación y correcciones manuales, enlaces a ADRs, auditorías y token-log.
  - Nota sobre las 7 reglas (el enunciado anuncia 8).
- **Payload:** `docs/adr/README.md`, `docs/ai/README.md`, `docs/ai/token-log.md`, `docs/IMPROVEMENTS.md`.
- **Scope:** `README.md`.
- **Depende de:** todas.
