# AGENTS.md

Inventory REST API (Redarbor technical test), built with the **SDAP** methodology (Spec-Driven Agentic Programming). This file is the entry point for any AI agent working in this repository.

## Sources of truth

| Layer | Path | Use |
|---|---|---|
| Layer 0 — immutable specification | `.sdap/` | Architecture and Tech Fence, domain rules, data contracts, use cases, API surface, sequence flows. **Never contradict it. Never edit it** (only exception: ticking a task checkbox in `.sdap/TASKS.md`). |
| Layer 1 — living code context | `docs/ai/` | How the code is built. Update it after your change (Reverse Sync). |
| Decisions | `docs/adr/` | Why things are the way they are. |
| Work queue | `.sdap/TASKS.md` | One atomic task at a time. |

**Load only what your task needs** (context injection protocol: `docs/ai/13-how-to-add-features.md`). Do not read the whole repository.

## Stack (summary — full Tech Fence in `.sdap/ARCH_SKELETON.md` §6)

- .NET 10, **`LangVersion` 12** (no C# 13/14 features), ASP.NET Core **Controllers**.
- CQRS with **Mediator** (martinothamar, MIT) + FluentValidation pipeline behavior.
- **Queries → EF Core** (`AsNoTracking`, projection to DTO, never `SaveChanges`).
- **Commands → Dapper** + `IUnitOfWork` transaction.
- SQL Server 2022, schema by **DbUp** scripts. **No `FOREIGN KEY`, ever.**
- Keycloak 26 (OAuth2/OIDC), JWT bearer validation.
- Tests: xUnit, NSubstitute, Shouldly, coverlet. Analyzers: StyleCop.
- **Forbidden:** MediatR ≥ 13, AutoMapper, Moq, FluentAssertions ≥ 8, EF migrations, EF writes, any package not in the Tech Fence.

## Architecture

```
Inventory.Api ──► Inventory.Application ──► Inventory.Domain
      └────────► Inventory.Infrastructure ──┘ (also ► Application)
```

- `Domain`: entities, enums, rules, domain exceptions. No dependencies.
- `Application`: commands, queries, handlers, validators, DTOs, ports, `ErrorMessages`.
- `Infrastructure`: EF read repositories, Dapper write repositories, DbUp scripts.
- `Api`: controllers, authorization policies, `ICurrentUser`, ProblemDetails, Swagger, DI.

## Commands

```bash
dotnet build                                   # must produce 0 warnings (warnings are errors)
dotnet test                                    # all tests green
dotnet test --collect:"XPlat Code Coverage"    # coverage (coverlet)
docker compose up --build                      # full stack: sqlserver + keycloak + api
```

## Clean Code rules (MANDATORY — the test is graded on them)

> The test statement announces 8 rules but lists 7. These are the 7, with their operational definition.

1. **English only.** All code, identifiers and comments are in English. The only exception is text returned to API clients, which is Spanish and lives exclusively in `Application/Common/Messages/ErrorMessages.cs`.
2. **No blank lines inside code blocks**, except exactly one blank line immediately before a `return` statement (and not when the `return` is the first statement of the block). One blank line between type members is allowed.
3. **Self-descriptive names.** A method name must make a comment unnecessary. No abbreviations (`qty`, `prod`, `mgr`).
4. **Single responsibility per method.** Violations:
   - a. more than **25 lines** in a method body;
   - b. loops or conditionals **nested more than once**;
   - c. sections that need a comment to be understood. Extract a well-named method instead.
5. **Configuration from environment variables.** No connection strings, URLs or secrets in `appsettings*.json` or code. Variable names: `.sdap/ARCH_SKELETON.md` §7.
6. **One type per file**: one `class`, `record`, `enum` or `interface` per file, and the file name equals the type name.
7. **Pass objects, not properties.** `repository.Save(product)`, never `repository.Save(id, name, sku, ...)`. Commands carry request objects.

## Guardrails (full list: `docs/ai/14-ai-rules.md`)

- **Ambiguity → stop and ask.** Never invent fields, defaults, validations, routes or error codes not in `.sdap/`.
- Only touch files inside the scope of the current task (`.sdap/INTERFACE_FLOW.md`, scope table).
- Do not modify `*.csproj`, `Directory.*.props`, `docker-compose.yml` or `Dockerfile` unless the task says so.
- Never edit a committed DbUp script: add a new one.
- No `// TODO`, no commented-out code, no dead code.
- Never write to `InventoryMovements` except by INSERT. Never edit `Products.Stock` outside `ApplyDelta`.

## Workflow per task (SDAP)

1. Pick the next task in `.sdap/TASKS.md` and load **only** its context payload.
2. **TDD:** write failing tests → commit `test(T-XX): ...` → implement → commit `feat(T-XX): ...` → refactor if needed (`refactor(T-XX): ...`).
3. `dotnet build` + `dotnet test` green.
4. Run the **governance audit** (`.claude/agents/governance-auditor.md`) over the task diff. Save its report to `docs/ai/audits/T-XX.md`. Fix every FAIL before closing.
5. **Reverse Sync:** update the affected `docs/ai/` files.
6. Mark the task as done in `TASKS.md`.

Commits follow Conventional Commits in English, referencing the task ID.
