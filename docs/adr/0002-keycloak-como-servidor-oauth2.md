# ADR-0002: Keycloak como servidor de autorización OAuth2

- **Estado:** Aceptada
- **Fecha:** 2026-10-22

## Contexto
Los endpoints deben protegerse con OAuth2. OAuth2 separa dos papeles: el **Authorization Server** (emite tokens) y el **Resource Server** (la API, que solo los valida).

## Decisión
Usar **Keycloak** en `docker-compose` como Authorization Server. La API actúa únicamente como Resource Server validando JWT (`AddJwtBearer` con `Authority` apuntando al realm).

## Alternativas
| Opción | Motivo de descarte |
|---|---|
| Endpoint propio que emite JWT | No es OAuth2 real, solo un JWT casero. |
| OpenIddict embebido | Introduce un servidor de autorización dentro de la API: más código propio, mezcla de responsabilidades (rompe SRP a nivel de sistema). |
| Duende IdentityServer | Requiere licencia comercial para empresas con ingresos superiores a 1M USD. |

## Consecuencias
- **Positivas:**
  - La API no gestiona usuarios ni contraseñas.
  - Scopes (`inventory.read`, `inventory.write`) y roles se definen fuera del código.
  - Swagger usa *Authorization Code + PKCE* (recomendado por OAuth 2.1; el *password grant* está desaconsejado).
  - El realm se exporta a `realm.json` y se importa al arrancar (`--import-realm`): reproducible con un solo comando.
- **Negativas:**
  - Contenedor pesado (~500 MB RAM, 20–40 s de arranque). Requiere `healthcheck` y `depends_on: condition: service_healthy`.
- Las pruebas unitarias no dependen de Keycloak; la autenticación se simula.
