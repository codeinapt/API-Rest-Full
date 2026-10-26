using Inventory.Api.Configuration;
using Inventory.Api.ErrorHandling;
using Inventory.Application.Common.Behaviors;
using Microsoft.AspNetCore.Authorization;

var builder = WebApplication.CreateBuilder(args);
var authenticationSettings = AuthenticationSettings.FromConfiguration(builder.Configuration);
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
var app = builder.Build();
app.UseExceptionHandler();
app.UseInventorySwagger();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health").AllowAnonymous();
app.Run();
