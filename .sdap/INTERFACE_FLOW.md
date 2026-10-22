# INTERFACE_FLOW — Flujos Técnicos de Interacción

> **Capa 0 SDAP · Inmutable.** Cada diagrama delimita el orden exacto de llamadas para una tarea atómica. El agente solo puede crear o modificar los componentes que aparecen en el diagrama asignado a su tarea.

---

## SEQ-01 · Autenticación desde Swagger UI (Authorization Code + PKCE)

```mermaid
sequenceDiagram
    autonumber
    actor U as Usuario
    participant SW as Swagger UI
    participant KC as Keycloak (localhost:8180)
    participant API as Inventory.Api
    U->>SW: Pulsa "Authorize"
    SW->>SW: Genera code_verifier y code_challenge (PKCE)
    SW->>KC: /auth?client_id=inventory-swagger&code_challenge=...
    KC->>U: Formulario de login
    U->>KC: Credenciales
    KC-->>SW: Redirección con authorization code
    SW->>KC: /token (code + code_verifier)
    KC-->>SW: access_token (JWT: iss, aud, sub, scope, roles, areas)
    U->>SW: Ejecuta una petición
    SW->>API: Petición + Authorization: Bearer JWT
    API->>KC: Metadatos y JWKS por red interna (cacheados)
    API->>API: Valida firma, issuer externo, audiencia, expiración
```

---

## SEQ-02 · Flujo genérico de Query (lectura con EF Core)

```mermaid
sequenceDiagram
    autonumber
    participant C as Controller
    participant M as Mediator
    participant VB as ValidationBehavior
    participant QH as QueryHandler
    participant RR as ReadRepository (EF Core)
    participant DB as SQL Server
    C->>M: Send(Query con objeto de parámetros)
    M->>VB: Pipeline
    VB->>VB: FluentValidation (paginación, filtros)
    VB->>QH: Handle
    QH->>RR: Consulta con filtros
    RR->>DB: SELECT con AsNoTracking y proyección a DTO
    DB-->>RR: Filas
    RR-->>QH: DTO o PagedResponse
    QH-->>C: Resultado
    C-->>C: 200 OK
```

---

## SEQ-03 · Flujo genérico de Command (escritura con Dapper)

```mermaid
sequenceDiagram
    autonumber
    participant C as Controller
    participant M as Mediator
    participant VB as ValidationBehavior
    participant CH as CommandHandler
    participant CU as ICurrentUser
    participant D as Entidad de dominio
    participant WR as WriteRepository (Dapper)
    participant UoW as IUnitOfWork
    participant DB as SQL Server
    C->>M: Send(Command con objeto Request)
    M->>VB: Pipeline
    VB->>CH: Handle (si la validación pasa)
    CH->>UoW: BeginTransaction
    CH->>WR: Cargar estado necesario
    WR->>DB: SELECT (en la transacción)
    CH->>CU: Subject / Roles / AreaCodes
    CH->>D: Ejecutar método de dominio (aplica reglas)
    CH->>WR: Persistir (sentencia condicional)
    WR->>DB: INSERT / UPDATE ... WHERE condición
    DB-->>WR: Filas afectadas
    CH->>UoW: Commit (o Rollback ante excepción)
    CH-->>C: Resultado
```

---

## SEQ-04 · CU-16 Registrar entrada / salida

```mermaid
sequenceDiagram
    autonumber
    participant C as ProductMovementsController
    participant H as RegisterMovementHandler
    participant PW as ProductWriteRepository
    participant MV as InventoryMovement (dominio)
    participant MW as MovementWriteRepository
    participant DB as SQL Server
    C->>H: RegisterMovementCommand(productId, request)
    H->>PW: GetStockSnapshot(productId)
    PW->>DB: SELECT Id, Stock, IsActive
    alt No existe
        H-->>C: PRODUCT_NOT_FOUND (404)
    end
    H->>MV: CreateInbound / CreateOutbound(snapshot, request, user)
    Note over MV: RM-01, RM-03, RM-08: errores comprensibles
    H->>PW: ApplyDelta(movement)
    PW->>DB: UPDATE Products SET Stock = Stock + @Delta OUTPUT deleted.Stock, inserted.Stock WHERE Id = @Id AND IsActive = 1 AND Stock + @Delta >= 0
    alt 0 filas (concurrencia)
        H-->>C: INSUFFICIENT_STOCK o PRODUCT_INACTIVE (409), rollback
    end
    H->>MW: Insert(movement con PreviousStock y ResultingStock del OUTPUT)
    MW->>DB: INSERT InventoryMovements
    H-->>C: MovementResponse (201)
```

> `PreviousStock` y `ResultingStock` se toman del `OUTPUT` del `UPDATE`, no de la lectura previa, para que sean exactos aunque haya concurrencia.

---

## SEQ-05 · CU-17 Registrar ajuste (con alcance por área)

```mermaid
sequenceDiagram
    autonumber
    participant C as ProductAdjustmentsController
    participant H as RegisterAdjustmentHandler
    participant PW as ProductWriteRepository
    participant AZ as AreaAccessPolicy
    participant CU as ICurrentUser
    participant MW as MovementWriteRepository
    participant DB as SQL Server
    C->>H: RegisterAdjustmentCommand(productId, request)
    H->>PW: GetSnapshotWithArea(productId)
    PW->>DB: SELECT p.Stock, p.IsActive, a.Code FROM Products p JOIN Categories c JOIN Areas a
    H->>AZ: EnsureCanSupervise(areaCode)
    AZ->>CU: AreaCodes
    alt Área no incluida
        H-->>C: AREA_ACCESS_DENIED (403)
    end
    opt relatedMovementId informado
        H->>MW: ExistsForProduct(relatedMovementId, productId)
        alt No existe o es de otro producto
            H-->>C: RELATED_MOVEMENT_INVALID (422)
        end
    end
    H->>PW: ApplyDelta (sin la condición IsActive: el ajuste se permite sobre inactivos)
    PW->>DB: UPDATE ... WHERE Id = @Id AND Stock + @Delta >= 0
    H->>MW: Insert(movement Type = Adjustment)
    H-->>C: MovementResponse (201)
```

---

## SEQ-06 · CU-12 / CU-13 Baja de producto

```mermaid
sequenceDiagram
    autonumber
    participant C as ProductsController
    participant H as DeleteProductHandler / DeactivateProductHandler
    participant P as Product (dominio)
    participant PW as ProductWriteRepository
    participant DB as SQL Server
    C->>H: Command(productId[, DeactivateProductRequest])
    H->>PW: GetById(productId)
    H->>P: Delete(user) / Deactivate(request, user)
    Note over P: RP-08 (stock 0 en DELETE), RP-09, RP-10
    H->>PW: SaveDeactivation(product)
    PW->>DB: UPDATE Products SET IsActive = 0, ... WHERE Id = @Id AND IsActive = 1 [AND Stock = 0 solo en DELETE]
    alt 0 filas (concurrencia)
        H-->>C: PRODUCT_HAS_STOCK / PRODUCT_INACTIVE (409)
    end
    H-->>C: 204
```

---

## SEQ-07 · CU-14 Reactivar producto

```mermaid
sequenceDiagram
    autonumber
    participant C as ProductsController
    participant H as ReactivateProductHandler
    participant PW as ProductWriteRepository
    participant AZ as AreaAccessPolicy
    participant DB as SQL Server
    C->>H: ReactivateProductCommand(productId)
    H->>PW: GetSnapshotWithArea(productId)
    H->>AZ: EnsureCanSupervise(areaCode)
    H->>H: RP-11 (debe estar inactivo)
    H->>PW: Reactivate(product)
    PW->>DB: UPDATE Products SET IsActive = 1, ReactivatedAt, ReactivatedBy WHERE Id = @Id AND IsActive = 0 AND EXISTS (categoría activa)
    alt Violación del índice único de SKU
        H-->>C: SKU_ALREADY_EXISTS (409)
    end
    H-->>C: 204
```

---

## SEQ-08 · Manejo de errores

```mermaid
sequenceDiagram
    autonumber
    participant API as Pipeline ASP.NET Core
    participant EH as ExceptionHandler (IExceptionHandler)
    participant MAP as ProblemDetailsMapper
    participant MSG as ErrorMessages
    API->>EH: Excepción (dominio, validación, no controlada)
    EH->>MAP: Map(excepción)
    MAP->>MSG: Texto en español según el code
    MAP-->>EH: ProblemDetails (status, title, detail, code[, errors])
    EH-->>API: Respuesta application/problem+json
    Note over EH: Las excepciones no controladas → 500 INTERNAL_ERROR, sin detalles internos en la respuesta y con log completo
```

---

## SEQ-09 · Arranque de la aplicación

```mermaid
sequenceDiagram
    autonumber
    participant DC as docker compose
    participant DB as sqlserver
    participant KC as keycloak
    participant API as api
    DC->>DB: Arrancar
    DC->>KC: Arrancar con --import-realm
    DB-->>DC: healthy
    KC-->>DC: healthy
    DC->>API: Arrancar (depends_on: service_healthy)
    API->>DB: Crear la base de datos si no existe y ejecutar scripts DbUp pendientes
    API->>API: Registrar DI, autenticación, políticas y Swagger
    API-->>DC: /health = 200
```

---

## Casos límite técnicos

| Caso | Tratamiento |
|---|---|
| Dos salidas concurrentes que juntas superan el stock | El `UPDATE ... WHERE Stock + @Delta >= 0` es atómico: una aplica y la otra recibe 0 filas → `INSUFFICIENT_STOCK`. |
| `DELETE` concurrente con una entrada | El `UPDATE ... AND Stock = 0` es atómico: si la entrada llega antes, el `DELETE` falla con `PRODUCT_HAS_STOCK`. |
| Desactivar una categoría mientras se crea un producto en ella | La comprobación del padre en escrituras usa `WITH (UPDLOCK, HOLDLOCK)` dentro de la transacción. |
| Dos creaciones con el mismo SKU, nombre o código simultáneas | El índice único filtrado rechaza la segunda. La violación (error SQL 2601/2627) se traduce a `*_ALREADY_EXISTS` (409). |
| Issuer distinto entre la red Docker y el navegador | `AUTH_METADATA_ADDRESS` (interno) para descargar metadatos y `AUTH_AUTHORITY` (externo) como `ValidIssuer`. |
| Keycloak tarda en arrancar | `depends_on: condition: service_healthy`. |
| Token sin claim `areas` | Se trata como lista vacía: cualquier operación con alcance por área → `AREA_ACCESS_DENIED`. |
| `page` más allá del total | `200` con `items: []`. |

---

## Delimitación de alcance por módulo

| Módulo | Diagramas | Puede tocar | Prohibido tocar |
|---|---|---|---|
| Areas | SEQ-02, SEQ-03 | `*/Areas/*`, `AreasController`, scripts de `Areas` | Otros módulos, `Common` sin autorización |
| Categories | SEQ-02, SEQ-03 | `*/Categories/*`, `CategoriesController` | Lógica de `Areas` (solo consulta su estado) |
| Products | SEQ-02, SEQ-03, SEQ-06, SEQ-07 | `*/Products/*`, `ProductsController` | Tabla `InventoryMovements`; edición directa de `Stock` |
| Movements | SEQ-04, SEQ-05 | `*/Movements/*`, `ProductMovementsController`, `ProductAdjustmentsController`, `ProductWriteRepository.ApplyDelta` | Cualquier `UPDATE` o `DELETE` sobre `InventoryMovements` |
| Seguridad | SEQ-01, SEQ-05 | `Api/Authorization/*`, `AreaAccessPolicy`, `ICurrentUser`, `keycloak/realm-inventory.json` | Lógica de negocio de los módulos |
| Errores | SEQ-08 | `Api/ErrorHandling/*`, `ErrorMessages` | Mensajes fuera de `ErrorMessages` |
| Infraestructura base | SEQ-09 | `docker-compose.yml`, `Dockerfile`, `Program.cs`, scripts DbUp | Scripts DbUp ya commiteados (se añade uno nuevo, nunca se edita) |
