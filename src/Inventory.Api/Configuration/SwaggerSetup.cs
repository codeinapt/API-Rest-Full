using Inventory.Api.Authorization;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Inventory.Api.Configuration;

public static class SwaggerSetup
{
    private const string OAuth2SchemeName = "oauth2";
    private const string KeycloakAuthorizationPath = "/protocol/openid-connect/auth";
    private const string KeycloakTokenPath = "/protocol/openid-connect/token";
    private static readonly string[] RequestedScopes = [ScopeNames.OpenId, ScopeNames.Read, ScopeNames.Write];

    public static IServiceCollection AddInventorySwagger(this IServiceCollection services, AuthenticationSettings settings)
    {
        services.AddSwaggerGen(options => AddOAuth2Security(options, settings));

        return services;
    }

    public static WebApplication UseInventorySwagger(this WebApplication app)
    {
        var clientId = app.Configuration.GetRequiredValue(EnvironmentVariableNames.SwaggerOAuthClientId);
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.OAuthClientId(clientId);
            options.OAuthUsePkce();
            options.OAuthScopes(RequestedScopes);
        });

        return app;
    }

    private static void AddOAuth2Security(SwaggerGenOptions options, AuthenticationSettings settings)
    {
        options.AddSecurityDefinition(OAuth2SchemeName, CreateOAuth2Scheme(settings));
        options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference(OAuth2SchemeName, document)] = [.. RequestedScopes],
        });
    }

    private static OpenApiSecurityScheme CreateOAuth2Scheme(AuthenticationSettings settings)
    {
        var authority = settings.Authority.TrimEnd('/');

        return new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.OAuth2,
            Flows = new OpenApiOAuthFlows
            {
                AuthorizationCode = new OpenApiOAuthFlow
                {
                    AuthorizationUrl = new Uri(authority + KeycloakAuthorizationPath),
                    TokenUrl = new Uri(authority + KeycloakTokenPath),
                    Scopes = new Dictionary<string, string>
                    {
                        [ScopeNames.OpenId] = "OpenID Connect",
                        [ScopeNames.Read] = "Read inventory data",
                        [ScopeNames.Write] = "Change inventory data",
                    },
                },
            },
        };
    }
}
