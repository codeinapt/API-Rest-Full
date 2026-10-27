using Inventory.Api.Configuration;
using Inventory.Api.ErrorHandling;
using Inventory.Application.Common.Behaviors;
using Inventory.Infrastructure;
using Inventory.Infrastructure.Migrations;
using Microsoft.AspNetCore.Authorization;

var builder = WebApplication.CreateBuilder(args);
var authenticationSettings = AuthenticationSettings.FromConfiguration(builder.Configuration);
var databaseConnectionString = builder.Configuration.GetRequiredValue(EnvironmentVariableNames.DatabaseConnection);
builder.Services.AddControllers();
builder.Services.AddHealthChecks();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddSingleton<IAuthorizationMiddlewareResultHandler, ProblemDetailsAuthorizationResultHandler>();
builder.Services.AddMediator(options =>
{
    options.ServiceLifetime = ServiceLifetime.Scoped;
    options.PipelineBehaviors = [typeof(ValidationBehavior<,>)];
});
builder.Services.AddInventoryAuthentication(authenticationSettings, builder.Environment);
builder.Services.AddInventoryAuthorization();
builder.Services.AddInventorySwagger(authenticationSettings);
builder.Services.AddInventoryInfrastructure(databaseConnectionString);
var app = builder.Build();
app.Services.GetRequiredService<DatabaseMigrator>().Migrate();
app.UseExceptionHandler();
app.UseInventorySwagger();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health").AllowAnonymous();
app.Run();
