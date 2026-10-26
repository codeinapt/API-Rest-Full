using Inventory.Api.Configuration;

var builder = WebApplication.CreateBuilder(args);
var authenticationSettings = AuthenticationSettings.FromConfiguration(builder.Configuration);
builder.Services.AddControllers();
builder.Services.AddHealthChecks();
builder.Services.AddInventoryAuthentication(authenticationSettings, builder.Environment);
builder.Services.AddInventoryAuthorization();
builder.Services.AddInventorySwagger(authenticationSettings);
var app = builder.Build();
app.UseInventorySwagger();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health").AllowAnonymous();
app.Run();
