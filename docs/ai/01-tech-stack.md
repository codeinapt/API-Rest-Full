# 01 · Stack técnico y dependencias (incluye SDAP-12)

> La lista de lo **permitido** es la Tech Fence de `.sdap/ARCH_SKELETON.md` §6. Este archivo registra **las versiones exactas en uso**.

## Fuente de verdad de versiones
- **Central Package Management:** todas las versiones NuGet viven en `Directory.Packages.props`. Los `.csproj` referencian paquetes **sin** versión.
- **Propiedades comunes** en `Directory.Build.props`: `TargetFramework=net10.0`, `LangVersion=12`, `Nullable=enable`, `ImplicitUsings=enable`, `TreatWarningsAsErrors=true`, StyleCop.
- **SDK fijado** en `global.json`.

## Versiones en uso
*(Se completa en T-00 con las últimas versiones estables disponibles en ese momento — Reverse Sync.)*

| Componente | Versión |
|---|---|
| .NET SDK | _pendiente T-00_ |
| Mediator.SourceGenerator / Abstractions | _pendiente T-00_ |
| FluentValidation | _pendiente T-00_ |
| Microsoft.EntityFrameworkCore.SqlServer | _pendiente T-00_ |
| Dapper | _pendiente T-00_ |
| Microsoft.Data.SqlClient | _pendiente T-00_ |
| dbup-sqlserver | _pendiente T-00_ |
| Microsoft.AspNetCore.Authentication.JwtBearer | _pendiente T-00_ |
| Swashbuckle.AspNetCore | _pendiente T-00_ |
| xUnit / NSubstitute / Shouldly / coverlet.collector | _pendiente T-00_ |
| StyleCop.Analyzers | _pendiente T-00_ |
| Imagen SQL Server | _pendiente T-01_ |
| Imagen Keycloak | _pendiente T-01_ |

## Política de dependencias
- Una dependencia nueva requiere un **ADR** y añadirla a la Tech Fence (la edita el humano).
- Criterios de aceptación de una dependencia:
  - licencia permisiva (MIT, Apache 2.0, BSD);
  - mantenimiento activo;
  - sin alternativa razonable en el framework.
- Las actualizaciones de versión mayor son tareas propias, nunca efectos colaterales de otra tarea.
