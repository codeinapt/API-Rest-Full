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

**Usuarios de prueba** (solo desarrollo; contraseñas documentadas en el README):

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

## Prohibido
- Leer el usuario o el área desde el body.
- Secretos en el código o en `appsettings`.
- Desactivar la validación de issuer, audiencia o firma.
