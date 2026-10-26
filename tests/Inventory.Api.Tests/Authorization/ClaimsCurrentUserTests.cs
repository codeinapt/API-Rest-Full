using System.Security.Claims;
using Inventory.Api.Authorization;
using Microsoft.AspNetCore.Http;

namespace Inventory.Api.Tests.Authorization;

public sealed class ClaimsCurrentUserTests
{
    [Fact]
    public void Subject_WhenSubjectClaimExists_ReturnsItsValue()
    {
        var currentUser = CreateCurrentUser(new Claim(KeycloakClaimTypes.Subject, "user-42"));
        currentUser.Subject.ShouldBe("user-42");
    }

    [Fact]
    public void Subject_WhenSubjectClaimIsMissing_ThrowsInvalidOperationException()
    {
        var currentUser = CreateCurrentUser();
        Should.Throw<InvalidOperationException>(() => currentUser.Subject);
    }

    [Fact]
    public void Subject_WhenThereIsNoHttpContext_ThrowsInvalidOperationException()
    {
        var currentUser = new ClaimsCurrentUser(new HttpContextAccessor());
        Should.Throw<InvalidOperationException>(() => currentUser.Subject);
    }

    [Fact]
    public void Roles_WhenRoleClaimsExist_ReturnsTheirValues()
    {
        var currentUser = CreateCurrentUser(
            new Claim(ClaimTypes.Role, RoleNames.Operator),
            new Claim(ClaimTypes.Role, RoleNames.Supervisor));
        currentUser.Roles.ShouldBe([RoleNames.Operator, RoleNames.Supervisor], ignoreOrder: true);
    }

    [Fact]
    public void Roles_WhenThereAreNoRoleClaims_ReturnsEmpty()
    {
        var currentUser = CreateCurrentUser();
        currentUser.Roles.ShouldBeEmpty();
    }

    [Fact]
    public void AreaCodes_WhenAreasClaimsExist_ReturnsTheirValues()
    {
        var currentUser = CreateCurrentUser(
            new Claim(KeycloakClaimTypes.Areas, "FOOD"),
            new Claim(KeycloakClaimTypes.Areas, "TOOLS"));
        currentUser.AreaCodes.ShouldBe(["FOOD", "TOOLS"], ignoreOrder: true);
    }

    [Fact]
    public void AreaCodes_WhenAreasClaimIsMissing_ReturnsEmpty()
    {
        var currentUser = CreateCurrentUser();
        currentUser.AreaCodes.ShouldBeEmpty();
    }

    [Fact]
    public void AreaCodes_WhenThereIsNoHttpContext_ReturnsEmpty()
    {
        var currentUser = new ClaimsCurrentUser(new HttpContextAccessor());
        currentUser.AreaCodes.ShouldBeEmpty();
    }

    private static ClaimsCurrentUser CreateCurrentUser(params Claim[] claims)
    {
        var httpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Bearer")) };

        return new ClaimsCurrentUser(new HttpContextAccessor { HttpContext = httpContext });
    }
}
