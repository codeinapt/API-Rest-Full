# 01 · Stack técnico y dependencias (incluye SDAP-12)

> La lista de lo **permitido** es la Tech Fence de `.sdap/ARCH_SKELETON.md` §6. Este archivo registra **las versiones exactas en uso**.

## Fuente de verdad de versiones
- **Central Package Management:** todas las versiones NuGet viven en `Directory.Packages.props`. Los `.csproj` referencian paquetes **sin** versión.
- **Propiedades comunes** en `Directory.Build.props`: `TargetFramework=net10.0`, `LangVersion=12`, `Nullable=enable`, `ImplicitUsings=enable`, `TreatWarningsAsErrors=true`, StyleCop.
- **SDK fijado** en `global.json`.

## Versiones en uso
Fijadas en T-00 (2026-10-23); imágenes Docker en T-01 (2026-10-25).

| Componente | Versión |
|---|---|
| .NET SDK | 10.0.108 (`global.json`, `rollForward: latestFeature`) |
| Mediator.SourceGenerator / Abstractions | 3.0.2 (generador en `Api`, abstracciones en `Application`) |
| FluentValidation | 12.2.0 |
| Microsoft.EntityFrameworkCore.SqlServer | 10.0.13 |
| Dapper | 2.1.89 |
| Microsoft.Data.SqlClient | 6.1.7 (ver nota 1) |
| dbup-sqlserver | 7.2.0 |
| Microsoft.AspNetCore.Authentication.JwtBearer | 10.0.13 |
| Swashbuckle.AspNetCore | 10.3.1 |
| xUnit / NSubstitute / Shouldly / coverlet.collector | xunit.v3.mtp-off 4.0.2 + xunit.runner.visualstudio 4.0.1 + Microsoft.NET.Test.Sdk 18.11.0 / 6.2.0 / 4.3.0 / 10.1.0 (ver nota 3) |
| Testcontainers.MsSql *(solo `Inventory.IntegrationTests`)* | 4.16.0 (ver nota 7) |
| StyleCop.Analyzers | 1.2.0-beta.556 (ver nota 4) |
| Imagen SQL Server | `mcr.microsoft.com/mssql/server:2022-CU27-GDR1-ubuntu-22.04` (ver nota 5) |
| Imagen Keycloak | `quay.io/keycloak/keycloak:26.8.1` (ver nota 5) |
| Imágenes .NET (Dockerfile) | build `mcr.microsoft.com/dotnet/sdk:10.0.402`, runtime `mcr.microsoft.com/dotnet/aspnet:10.0.13` (ver nota 6) |
| System.Security.Cryptography.Pkcs *(pin transitivo)* | 10.0.13 (ver nota 2) |

### Notas sobre versiones
1. **SqlClient 6.1.7 y no 7.x:** es la versión con la que se compila y prueba EF Core 10.0.13 (y la que pide `dbup-sqlserver`). Subir de versión mayor solo en Dapper rompería la coherencia del proceso.
2. **Pin transitivo de seguridad:** SqlClient 6.1.7 arrastra `System.Security.Cryptography.Pkcs` 9.0.11 (GHSA-846f-f764-76mx, GHSA-2fmv-rv2c-59x7), y el build falla por `NU1903` con `TreatWarningsAsErrors`. Se fija 10.0.13 con `CentralPackageTransitivePinningEnabled`. No es tecnología nueva: es una librería de Microsoft que ya estaba en el grafo. Se puede eliminar cuando SqlClient incluya una versión corregida.
3. **`xunit.v3.mtp-off`:** xUnit v3 4.x funciona por defecto sobre Microsoft Testing Platform, y el SDK de .NET 10 rechaza ejecutarlo con VSTest. La variante oficial `mtp-off` (el mismo xUnit v3) mantiene VSTest, de modo que `coverlet.collector` y `dotnet test --collect:"XPlat Code Coverage"` siguen funcionando.
4. **StyleCop en beta:** la última estable (1.1.118, de 2019) da falsos positivos con C# 9–12 que el build trata como errores: SA1313 en *records* posicionales, SA1010 en *collection expressions* y SA1516 en *top-level statements*. La 1.2.0-beta.556 es la versión que usa la comunidad desde hace años. Las betas 1.3.x tenían solo unos días en el momento de elegir.
5. **Etiquetas exactas de imagen:** `ARCH_SKELETON` §8 nombra las líneas (`mssql/server:2022`, `keycloak:26`), pero esas etiquetas no existen como tales. Se fija la última versión concreta de cada línea para que el arranque sea reproducible.
6. **SDK de la imagen de build:** no hay imagen `sdk:10.0.108`. La `10.0.402` cumple `global.json` (`rollForward: latestFeature`). El runtime `aspnet:10.0.13` coincide con los paquetes `10.0.13`.
7. **Testcontainers.MsSql 4.16.0:** última estable al elegir (publicada el 2026-10-08). Ya estaba en la Tech Fence (`ARCH_SKELETON` §6) y en ADR-0012, así que no requiere ADR nuevo. El contenedor usa la misma imagen exacta que `docker-compose.yml` (nota 5), para probar contra la misma versión de SQL Server.

## Política de dependencias
- Una dependencia nueva requiere un **ADR** y añadirla a la Tech Fence (la edita el humano).
- Criterios de aceptación de una dependencia:
  - licencia permisiva (MIT, Apache 2.0, BSD);
  - mantenimiento activo;
  - sin alternativa razonable en el framework.
- Las actualizaciones de versión mayor son tareas propias, nunca efectos colaterales de otra tarea.
