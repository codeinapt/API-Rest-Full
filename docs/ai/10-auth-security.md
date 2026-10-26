# 10 · Autenticación, autorización y seguridad

Decisiones: ADR-0002, ADR-0009. Reglas: `.sdap/DOMAIN_LOGIC.md` RS-01 a RS-06 y §4. Políticas: `.sdap/SCREEN_FLOW.md` §2.

## Keycloak (`keycloak/realm-inventory.json`)
| Elemento | Valor |
|---|---|
| Realm | `inventory` |
| Cliente API | `inventory-api`: *bearer-only*; es la audiencia del token |
| Cliente Swagger | `inventory-swagger`: público, Authorization Code + PKCE (S256); *redirect* `http://localhost:8080/swagger/oauth2-redirect.html` |
| Client scopes | `inventory.read`, `inventory.write` |
| Realm roles | `operator`, `supervisor`, `admin` |
| Grupos | `/areas/FOOD`, `/areas/CLEAN`, `/areas/TOOLS` |
| Mappers | *Audience* `inventory-api`; claim `areas` = último segmento del nombre de los grupos del usuario |

**Notas de implementación del realm** (T-02):
- El realm se importa al arrancar (`start-dev --import-realm`) desde `keycloak/realm-inventory.json`. Si el realm ya existe, la importación no hace nada; para recargarlo hay que recrear el contenedor.
- El JSON define también los client scopes de serie `basic` (claim `sub`) y `roles` (`realm_access.roles`). Cuando un realm declara `clientScopes`, Keycloak no crea los de serie, y sin estos dos el token no lleva ni `sub` ni roles. **No se deben borrar.**
- Los usuarios tienen `email`, `firstName` y `lastName` porque el *user profile* por defecto de Keycloak 26 los exige. Sin ellos, el login se detiene en la pantalla "actualizar cuenta".
- El *password grant* está desactivado (`directAccessGrantsEnabled: false`): los tokens solo se obtienen con Authorization Code + PKCE.

**Usuarios de prueba** (solo desarrollo; contraseña común `Inventory123!`, documentada también en el README):

| Usuario | Rol | Áreas |
|---|---|---|
| `operator1` | operator | — |
| `supervisor.food` | supervisor | FOOD |
| `supervisor.tools` | supervisor | TOOLS |
| `admin1` | admin | — |

## API
- `AddJwtBearer`:
  - `MetadataAddress = AUTH_METADATA_ADDRESS` (red interna);
  - `ValidIssuer = AUTH_AUTHORITY` (externo);
  - `ValidAudience = AUTH_AUDIENCE`;
  - `RequireHttpsMetadata = false` **solo** en `Development`.
- **Roles de Keycloak:** vienen en `realm_access.roles`. Un `IClaimsTransformation` (`KeycloakRolesClaimsTransformation`) los convierte en claims de rol estándar para que funcionen las políticas.
- **Scopes:** vienen en la claim `scope`, separados por espacios.
- **Políticas** (`CanRead`, `CanOperate`, `CanSupervise`, `CanAdminister`) registradas con nombres constantes y aplicadas con `[Authorize(Policy = ...)]` en cada acción. **No existe ningún endpoint de negocio sin política.**
- `ICurrentUser` (`ClaimsCurrentUser`) expone `Subject`, `Roles` y `AreaCodes`.
- **Alcance por área:** `AreaAccessPolicy.EnsureCanSupervise(areaCode)` en `Application`. Si el usuario no tiene la claim `areas`, se trata como lista vacía.

## Swagger
Esquema de seguridad OAuth2 (`authorizationCode`) con los scopes `openid`, `inventory.read` e `inventory.write`; `UsePkce()`; `client_id` desde `SWAGGER_OAUTH_CLIENT_ID`.

**Notas de implementación** (T-03):
- Registro en `Api/Configuration`: `AuthenticationSetup`, `AuthorizationSetup` y `SwaggerSetup`. Las variables de entorno se leen al arrancar con `AuthenticationSettings.FromConfiguration` y `GetRequiredValue`; si falta una, la API no arranca (`InvalidOperationException`).
- `MapInboundClaims = false`: las claims conservan el nombre del JWT (`sub`, `scope`, `realm_access`, `areas`). Los nombres están en `KeycloakClaimTypes`.
- `KeycloakRolesClaimsTransformation` clona el principal y añade una claim `ClaimTypes.Role` por cada rol de `realm_access.roles`. Es idempotente, porque ASP.NET Core puede invocarla varias veces por petición.
- Las políticas se definen en `Authorization/InventoryPolicies.cs` (`AddInventoryPolicies`). Todas exigen usuario autenticado. El scope se comprueba con `HasScope` (`ScopeClaimsPrincipalExtensions`), que trocea la claim `scope` por espacios y compara de forma exacta y ordinal.
- `ClaimsCurrentUser.Subject` lanza `InvalidOperationException` si no hay `sub`, porque solo se usa tras una política. `Roles` y `AreaCodes` devuelven una lista vacía si no hay claims.
- En `docker-compose.yml` la API corre con `ASPNETCORE_ENVIRONMENT=Development`, porque los metadatos internos (`http://keycloak:8080`) son HTTP.
- Swagger UI se publica en todos los entornos (`/swagger`, anónimo): es el único cliente visual. Las URLs de autorización y de token se derivan de `AUTH_AUTHORITY` (`/protocol/openid-connect/auth` y `/token`).

## Prohibido
- Leer el usuario o el área desde el body.
- Secretos en el código o en `appsettings`.
- Desactivar la validación de issuer, audiencia o firma.
