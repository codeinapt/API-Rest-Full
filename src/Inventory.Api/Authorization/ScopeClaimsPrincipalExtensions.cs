using System.Security.Claims;

namespace Inventory.Api.Authorization;

public static class ScopeClaimsPrincipalExtensions
{
    public static bool HasScope(this ClaimsPrincipal principal, string scope)
    {
        return principal
            .FindAll(KeycloakClaimTypes.Scope)
            .SelectMany(claim => claim.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .Contains(scope, StringComparer.Ordinal);
    }
}
