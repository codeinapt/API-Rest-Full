using System.Security.Claims;
using Inventory.Api.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace Inventory.Api.Tests.Authorization;

public sealed class InventoryPoliciesTests
{
    private readonly IAuthorizationService authorizationService = CreateAuthorizationService();

    [Theory]
    [InlineData(PolicyNames.CanRead, ScopeNames.Read, null)]
    [InlineData(PolicyNames.CanOperate, ScopeNames.Write, RoleNames.Operator)]
    [InlineData(PolicyNames.CanOperate, ScopeNames.Write, RoleNames.Supervisor)]
    [InlineData(PolicyNames.CanSupervise, ScopeNames.Write, RoleNames.Supervisor)]
    [InlineData(PolicyNames.CanAdminister, ScopeNames.Write, RoleNames.Admin)]
    public async Task AuthorizeAsync_WhenScopeAndRoleMeetThePolicy_Succeeds(string policyName, string scope, string? role)
    {
        var user = CreateAuthenticatedUser(scope, role);
        var result = await authorizationService.AuthorizeAsync(user, policyName);
        result.Succeeded.ShouldBeTrue();
    }

    [Theory]
    [InlineData(PolicyNames.CanRead, ScopeNames.Write, RoleNames.Admin)]
    [InlineData(PolicyNames.CanOperate, ScopeNames.Read, RoleNames.Operator)]
    [InlineData(PolicyNames.CanOperate, ScopeNames.Write, RoleNames.Admin)]
    [InlineData(PolicyNames.CanSupervise, ScopeNames.Write, RoleNames.Operator)]
    [InlineData(PolicyNames.CanSupervise, ScopeNames.Read, RoleNames.Supervisor)]
    [InlineData(PolicyNames.CanAdminister, ScopeNames.Write, RoleNames.Supervisor)]
    [InlineData(PolicyNames.CanAdminister, ScopeNames.Read, RoleNames.Admin)]
    public async Task AuthorizeAsync_WhenScopeOrRoleIsMissing_Fails(string policyName, string scope, string role)
    {
        var user = CreateAuthenticatedUser(scope, role);
        var result = await authorizationService.AuthorizeAsync(user, policyName);
        result.Succeeded.ShouldBeFalse();
    }

    [Fact]
    public async Task AuthorizeAsync_WhenUserIsAnonymous_Fails()
    {
        var anonymousUser = new ClaimsPrincipal(new ClaimsIdentity([new Claim(KeycloakClaimTypes.Scope, ScopeNames.Read)]));
        var result = await authorizationService.AuthorizeAsync(anonymousUser, PolicyNames.CanRead);
        result.Succeeded.ShouldBeFalse();
    }

    private static IAuthorizationService CreateAuthorizationService()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAuthorizationBuilder().AddInventoryPolicies();

        return services.BuildServiceProvider().GetRequiredService<IAuthorizationService>();
    }

    private static ClaimsPrincipal CreateAuthenticatedUser(string scope, string? role)
    {
        var claims = new List<Claim> { new(KeycloakClaimTypes.Scope, $"openid {scope}") };
        claims.AddRange(RoleClaims(role));

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Bearer"));
    }

    private static IEnumerable<Claim> RoleClaims(string? role)
    {
        return role is null ? [] : [new Claim(ClaimTypes.Role, role)];
    }
}
