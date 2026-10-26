using System.Security.Claims;
using Inventory.Api.Authorization;

namespace Inventory.Api.Tests.Authorization;

public sealed class ScopeClaimsPrincipalExtensionsTests
{
    [Theory]
    [InlineData("inventory.read")]
    [InlineData("openid inventory.read profile")]
    [InlineData("openid  inventory.read")]
    public void HasScope_WhenScopeIsInTheSpaceSeparatedList_ReturnsTrue(string scopeClaimValue)
    {
        var principal = CreatePrincipal(new Claim(KeycloakClaimTypes.Scope, scopeClaimValue));
        principal.HasScope(ScopeNames.Read).ShouldBeTrue();
    }

    [Theory]
    [InlineData("openid inventory.write")]
    [InlineData("openid inventory.reader")]
    [InlineData("")]
    public void HasScope_WhenScopeIsNotInTheList_ReturnsFalse(string scopeClaimValue)
    {
        var principal = CreatePrincipal(new Claim(KeycloakClaimTypes.Scope, scopeClaimValue));
        principal.HasScope(ScopeNames.Read).ShouldBeFalse();
    }

    [Fact]
    public void HasScope_WhenScopeClaimIsMissing_ReturnsFalse()
    {
        var principal = CreatePrincipal();
        principal.HasScope(ScopeNames.Read).ShouldBeFalse();
    }

    private static ClaimsPrincipal CreatePrincipal(params Claim[] claims)
    {
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Bearer"));
    }
}
