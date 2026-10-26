using System.Security.Claims;
using Inventory.Api.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace Inventory.Api.Configuration;

public static class AuthenticationSetup
{
    public static IServiceCollection AddInventoryAuthentication(this IServiceCollection services, AuthenticationSettings settings, IHostEnvironment environment)
    {
        services.AddSingleton<IClaimsTransformation, KeycloakRolesClaimsTransformation>();
        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options => ConfigureJwtBearer(options, settings, environment));

        return services;
    }

    private static void ConfigureJwtBearer(JwtBearerOptions options, AuthenticationSettings settings, IHostEnvironment environment)
    {
        options.MetadataAddress = settings.MetadataAddress;
        options.RequireHttpsMetadata = !environment.IsDevelopment();
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = settings.Authority,
            ValidAudience = settings.Audience,
            NameClaimType = KeycloakClaimTypes.Subject,
            RoleClaimType = ClaimTypes.Role,
        };
    }
}
