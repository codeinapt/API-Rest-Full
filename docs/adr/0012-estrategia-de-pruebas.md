# ADR-0012: Estrategia de pruebas y TDD

- **Estado:** Aceptada (se detallará en la Capa 1: `docs/ai/11-testing-strategy.md`)
- **Fecha:** 2026-10-22

## Contexto
Se evalúan TDD, cobertura y calidad de las pruebas unitarias.

## Decisión
- **Pruebas unitarias:**
  - reglas de dominio (R1–R8 y reglas de desactivación);
  - validadores;
  - handlers de Commands y Queries con repositorios simulados;
  - *authorization handler* de área;
  - mapeo de excepciones a ProblemDetails.
- **Fuera de las unitarias:** SQL de Dapper y consultas EF. Se cubren, como extra, con pruebas de integración contra SQL Server real en contenedor (Testcontainers).
- **TDD visible en el historial:** por cada regla, un commit `test:` (rojo) seguido de un commit `feat:` (verde).
- **Cobertura:** objetivo de ~80 % en `Domain` y `Application`, medida con coverlet.
- **Herramientas:** xUnit, **NSubstitute**, **Shouldly**.

## Alternativas
- **Moq:** descartado por la polémica de SponsorLink (2023).
- **FluentAssertions:** descartado; desde la v8 requiere licencia comercial.

## Consecuencias
- La elección de dependencias se justifica también por licencias y confianza en la cadena de suministro.
