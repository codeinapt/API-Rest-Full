using Inventory.Api.Authorization;
using Inventory.Application.Common.Interfaces;

namespace Inventory.Api.Configuration;

public static class AuthorizationSetup
{
    public static IServiceCollection AddInventoryAuthorization(this IServiceCollection services)
    {
        services.AddAuthorizationBuilder().AddInventoryPolicies();
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, ClaimsCurrentUser>();

        return services;
    }
}
