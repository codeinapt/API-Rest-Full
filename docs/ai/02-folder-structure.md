# 02 · Estructura de carpetas y ubicación de archivos

Visión de alto nivel: `.sdap/ARCH_SKELETON.md` §5. Aquí se detalla **dónde va cada tipo de archivo**.

## Inventory.Domain
```
{Module}/                 Areas | Categories | Products | Movements
  {Entity}.cs             Entidad con sus métodos de dominio
  {Enum}.cs               Un enum por archivo
  {Entity}{Concept}.cs    Record de dominio que agrupa datos de entrada (ProductDetails, ProductDeactivation)
  Exceptions/{Name}Exception.cs
Common/
  InventoryException.cs   Base abstracta: Code (ErrorCode) + MessageArguments
  DomainException.cs      Hereda de InventoryException (→ 409)
  ErrorCode.cs            Enum de los códigos de DOMAIN_LOGIC §6
```

## Inventory.Application
```
{Module}/
  Commands/{Action}{Entity}/          p. ej. Commands/CreateProduct/
    {Action}{Entity}Command.cs
    {Action}{Entity}Handler.cs
    {Action}{Entity}Validator.cs
  Queries/{Get...}/
    {Get...}Query.cs
    {Get...}Handler.cs
    {Get...}Validator.cs              (si recibe parámetros)
    {Entity}QueryParameters.cs        (en la carpeta del listado, p. ej. Queries/GetAreas/)
    {Entity}Sort{Concept}.cs          Enums de orden del listado (ProductSortField, ProductSortDirection en Queries/GetProducts/)
  Dtos/{Name}Request.cs | {Name}Response.cs
  Ports/I{Entity}ReadRepository.cs | I{Entity}WriteRepository.cs
Common/
  Behaviors/ValidationBehavior.cs
  Interfaces/ICurrentUser.cs, IClock.cs, IUnitOfWork.cs
  Exceptions/                         Excepciones de aplicación (NotFound, Forbidden…)
  Pagination/PagedResponse.cs, PaginationParameters.cs, PaginationParametersValidator.cs,
             StatusFilter.cs          (filtro status compartido: Active | Inactive | All)
  Messages/ErrorMessages.cs           ÚNICO lugar con texto en español
  Authorization/AreaAccessPolicy.cs
```

## Inventory.Infrastructure
```
Persistence/
  Read/InventoryReadDbContext.cs
  Read/Models/{Table}Row.cs           Filas de solo lectura (AreaRow, CategoryRow, ProductRow, InventoryMovementRow)
  Read/Configurations/{Table}RowConfiguration.cs, UtcDateTimeConverter.cs
  Read/{Entity}ReadRepository.cs
  Write/SqlConnectionFactory.cs, UnitOfWork.cs
  Write/{Entity}WriteRepository.cs
Migrations/
  Scripts/NNNN_Description.sql       Recurso embebido
  DatabaseMigrator.cs
Common/SystemClock.cs                 Implementación de IClock
DependencyInjection.cs
```

## Inventory.Api
```
Controllers/{Resource}Controller.cs
Authorization/{Policy}Names.cs, ClaimsCurrentUser.cs, KeycloakRolesClaimsTransformation.cs
ErrorHandling/GlobalExceptionHandler.cs, ProblemDetailsMapper.cs, ErrorCatalog.cs, ErrorText.cs,
              ProblemDetailsResponseWriter.cs, ProblemDetailsAuthorizationResultHandler.cs
Configuration/{Area}Setup.cs         Extensiones de registro (Swagger, Auth, …)
Program.cs
Dockerfile
```

## Tests
Las carpetas de prueba replican la ruta del código probado: `tests/Inventory.Domain.Tests/Products/ProductTests.cs`.
