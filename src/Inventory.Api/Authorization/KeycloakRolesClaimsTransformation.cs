using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;

namespace Inventory.Api.Authorization;

public sealed class KeycloakRolesClaimsTransformation : IClaimsTransformation
{
    private const string RolesProperty = "roles";

    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity is not ClaimsIdentity { IsAuthenticated: true })
        {
            return Task.FromResult(principal);
        }
        var transformed = principal.Clone();
        var identity = (ClaimsIdentity)transformed.Identity!;
        var missingRoleClaims = ReadRealmRoles(identity)
            .Where(role => !identity.HasClaim(ClaimTypes.Role, role))
            .Select(role => new Claim(ClaimTypes.Role, role))
            .ToList();
        identity.AddClaims(missingRoleClaims);

        return Task.FromResult(transformed);
    }

    private static IEnumerable<string> ReadRealmRoles(ClaimsIdentity identity)
    {
        var realmAccess = identity.FindFirst(KeycloakClaimTypes.RealmAccess);
        if (realmAccess is null)
        {
            return [];
        }
        using var document = JsonDocument.Parse(realmAccess.Value);

        return ReadRolesProperty(document.RootElement);
    }

    private static List<string> ReadRolesProperty(JsonElement realmAccess)
    {
        if (!realmAccess.TryGetProperty(RolesProperty, out var roles))
        {
            return [];
        }

        return roles.EnumerateArray().Select(role => role.GetString()).OfType<string>().ToList();
    }
}
