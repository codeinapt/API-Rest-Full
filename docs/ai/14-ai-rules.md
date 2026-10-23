# 14 · Reglas para la IA (Guardrails)

Lista completa de barreras de contención. `AGENTS.md` contiene el resumen.

## Especificación
1. **Prohibido suponer.** Si un requisito, caso de uso o contrato es ambiguo o falta, **detén la ejecución y pregunta**. No inventes campos opcionales, valores por defecto, validaciones, permisos, rutas ni códigos de error.
2. **`.sdap/` es de solo lectura** para los agentes. Única excepción: marcar `[x]` una tarea completada en `.sdap/TASKS.md`. Si la tarea parece requerir un cambio de especificación, se detiene y se propone al humano.
3. Ninguna ruta que no esté en `.sdap/SCREEN_FLOW.md` §3. Ningún código de error que no esté en `.sdap/DOMAIN_LOGIC.md` §6.
4. Ninguna refactorización altera tablas, DTOs públicos ni firmas de puertos definidas en `.sdap/DATA_MINDMAP.md`.

## Alcance
5. Solo se crean o modifican archivos dentro del **Scope** de la tarea. Si algo fuera del alcance está roto, se reporta y no se arregla.
6. Prohibido modificar `*.csproj`, `Directory.Build.props`, `Directory.Packages.props`, `global.json`, `.editorconfig`, `docker-compose.yml`, `Dockerfile` o `keycloak/realm-inventory.json` salvo que la tarea lo indique.
7. Prohibido añadir paquetes fuera de la Tech Fence.

## Datos e integridad
8. Jamás `FOREIGN KEY`.
9. Jamás `UPDATE` ni `DELETE` sobre `InventoryMovements`.
10. Jamás modificar `Products.Stock` fuera de `ProductWriteRepository.ApplyDelta`.
11. Jamás editar un script DbUp ya commiteado.
12. Jamás escribir con EF Core ni leer con Dapper fuera de un Command.
13. SQL siempre parametrizado.

## Seguridad
14. Ningún endpoint de negocio sin `[Authorize(Policy = ...)]`.
15. El usuario, sus roles y sus áreas salen del token (`ICurrentUser`), nunca del body.
16. Ningún secreto en el código ni en `appsettings*.json`.

## Calidad
17. Nada de `// TODO`, código comentado, código muerto ni `#pragma warning disable` sin justificación aprobada.
18. Toda implementación está completa o falla de forma explícita con una excepción tipada.
19. No se desactivan ni borran tests para conseguir el verde. Un test existente solo cambia si la tarea cambia el comportamiento que prueba.
20. Las 7 reglas de Clean Code de `AGENTS.md` se aplican también al código de prueba.
