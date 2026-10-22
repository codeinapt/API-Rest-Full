# ADR-0001: .NET 10 LTS como plataforma

- **Estado:** Aceptada
- **Fecha:** 2026-10-22

## Contexto
El enunciado pide ".NET 6.0 o .NET Core". ".NET Core" y ".NET 5+" son la misma línea de producto: tras .NET Core 3.1, Microsoft eliminó "Core" del nombre. El estado del soporte a la fecha:

| Versión | Fin de soporte |
|---|---|
| .NET 6 (LTS) | Noviembre 2024 — sin soporte |
| .NET 8 (LTS) | 10 de noviembre de 2026 |
| .NET 10 (LTS) | Noviembre 2028 |

## Decisión
Usar **.NET 10 LTS**.

## Justificación
- **No contradice el enunciado:** .NET 10 pertenece a la familia ".NET Core" moderna y multiplataforma.
- **Proyecto nuevo (*greenfield*):** no existe código legacy que obligue a mantener una versión. Iniciar en una versión sin parches de seguridad sería deuda técnica desde el día uno.
- **En un sistema existente la decisión sería otra:** se respetaría la versión vigente y la migración se planificaría como proyecto con coste/beneficio.

## Alternativas
- **.NET 6:** cumplimiento literal, pero sin soporte de seguridad.
- **.NET 8:** ecosistema maduro, pero su soporte termina semanas después de la entrega.

## Consecuencias
- **Reversibilidad garantizada (Tech Fence):** `<LangVersion>12</LangVersion>` hace que el compilador impida usar características de C# 13/14. Bajar a .NET 8 consiste en cambiar `TargetFramework` y versiones de paquetes EF Core.
- La ejecución vía Docker no requiere .NET 10 instalado en el equipo; solo la depuración local requiere el SDK 10.
