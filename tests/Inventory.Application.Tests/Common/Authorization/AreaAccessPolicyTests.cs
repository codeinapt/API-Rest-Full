using Inventory.Application.Common.Authorization;
using Inventory.Application.Common.Exceptions;
using Inventory.Application.Common.Interfaces;
using Inventory.Domain.Common;
using NSubstitute;

namespace Inventory.Application.Tests.Common.Authorization;

public sealed class AreaAccessPolicyTests
{
    private readonly ICurrentUser currentUser = Substitute.For<ICurrentUser>();
    private readonly AreaAccessPolicy policy;

    public AreaAccessPolicyTests()
    {
        policy = new AreaAccessPolicy(currentUser);
    }

    [Fact]
    public void EnsureCanSupervise_WhenAreaIsInUserAreas_DoesNotThrow()
    {
        currentUser.AreaCodes.Returns(["CLEAN", "FOOD"]);
        Should.NotThrow(() => policy.EnsureCanSupervise("FOOD"));
    }

    [Theory]
    [InlineData("TOOLS")]
    [InlineData("food")]
    public void EnsureCanSupervise_WhenAreaIsNotInUserAreas_ThrowsAreaAccessDeniedWithAreaCode(string areaCode)
    {
        currentUser.AreaCodes.Returns(["FOOD"]);
        var exception = Should.Throw<ForbiddenException>(() => policy.EnsureCanSupervise(areaCode));
        exception.Code.ShouldBe(ErrorCode.AreaAccessDenied);
        exception.MessageArguments.ShouldBe([areaCode]);
    }

    [Fact]
    public void EnsureCanSupervise_WhenUserHasNoAreas_ThrowsAreaAccessDenied()
    {
        currentUser.AreaCodes.Returns([]);
        var exception = Should.Throw<ForbiddenException>(() => policy.EnsureCanSupervise("FOOD"));
        exception.Code.ShouldBe(ErrorCode.AreaAccessDenied);
    }
}
