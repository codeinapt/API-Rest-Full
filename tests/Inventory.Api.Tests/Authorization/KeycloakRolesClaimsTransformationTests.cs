using System.Security.Claims;
using Inventory.Api.Authorization;

namespace Inventory.Api.Tests.Authorization;

public sealed class KeycloakRolesClaimsTransformationTests
{
    private readonly KeycloakRolesClaimsTransformation transformation = new();

    [Fact]
    public async Task TransformAsync_WhenRealmAccessHasRoles_AddsStandardRoleClaims()
    {
        var principal = CreateAuthenticatedPrincipal(RealmAccessClaim("""{"roles":["operator","supervisor"]}"""));
        var transformed = await transformation.TransformAsync(principal);
        transformed.IsInRole(RoleNames.Operator).ShouldBeTrue();
        transformed.IsInRole(RoleNames.Supervisor).ShouldBeTrue();
    }

    [Fact]
    public async Task TransformAsync_WhenRealmAccessIsMissing_AddsNoRoleClaims()
    {
        var principal = CreateAuthenticatedPrincipal();
        var transformed = await transformation.TransformAsync(principal);
        transformed.FindAll(ClaimTypes.Role).ShouldBeEmpty();
    }

    [Fact]
    public async Task TransformAsync_WhenRealmAccessHasNoRolesProperty_AddsNoRoleClaims()
    {
        var principal = CreateAuthenticatedPrincipal(RealmAccessClaim("""{"groups":["areas"]}"""));
        var transformed = await transformation.TransformAsync(principal);
        transformed.FindAll(ClaimTypes.Role).ShouldBeEmpty();
    }

    [Fact]
    public async Task TransformAsync_WhenCalledTwice_DoesNotDuplicateRoleClaims()
    {
        var principal = CreateAuthenticatedPrincipal(RealmAccessClaim("""{"roles":["admin"]}"""));
        var transformedOnce = await transformation.TransformAsync(principal);
        var transformedTwice = await transformation.TransformAsync(transformedOnce);
        transformedTwice.FindAll(ClaimTypes.Role).Count().ShouldBe(1);
    }

    [Fact]
    public async Task TransformAsync_WhenPrincipalIsNotAuthenticated_AddsNoRoleClaims()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity([RealmAccessClaim("""{"roles":["admin"]}""")]));
        var transformed = await transformation.TransformAsync(principal);
        transformed.FindAll(ClaimTypes.Role).ShouldBeEmpty();
    }

    private static Claim RealmAccessClaim(string json)
    {
        return new Claim(KeycloakClaimTypes.RealmAccess, json);
    }

    private static ClaimsPrincipal CreateAuthenticatedPrincipal(params Claim[] claims)
    {
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Bearer"));
    }
}
