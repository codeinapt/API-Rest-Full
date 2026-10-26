using System.Security.Claims;
using Inventory.Application.Common.Interfaces;

namespace Inventory.Api.Authorization;

public sealed class ClaimsCurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    public string Subject =>
        User?.FindFirst(KeycloakClaimTypes.Subject)?.Value
        ?? throw new InvalidOperationException("The current request has no authenticated subject.");

    public IReadOnlyCollection<string> Roles => ReadClaimValues(ClaimTypes.Role);

    public IReadOnlyCollection<string> AreaCodes => ReadClaimValues(KeycloakClaimTypes.Areas);

    private ClaimsPrincipal? User => httpContextAccessor.HttpContext?.User;

    private List<string> ReadClaimValues(string claimType)
    {
        return User?.FindAll(claimType).Select(claim => claim.Value).ToList() ?? [];
    }
}
