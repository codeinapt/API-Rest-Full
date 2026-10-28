using System.Reflection;
using Inventory.Api.Authorization;
using Inventory.Api.Controllers;
using Inventory.Application.Common.Pagination;
using Inventory.Application.Movements.Commands.RegisterMovement;
using Inventory.Application.Movements.Dtos;
using Inventory.Application.Movements.Queries.GetMovements;
using Inventory.Domain.Movements;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace Inventory.Api.Tests.Controllers;

public sealed class ProductMovementsControllerTests
{
    private const int ProductId = 21;
    private static readonly MovementResponse Movement = new(5, ProductId, MovementType.In, 10, 0, 10, null, null, null, new DateTime(2026, 10, 1, 8, 0, 0, DateTimeKind.Utc), "operator-sub");

    private readonly ISender sender = Substitute.For<ISender>();
    private readonly ProductMovementsController controller;

    public ProductMovementsControllerTests()
    {
        controller = new ProductMovementsController(sender);
    }

    [Theory]
    [InlineData(nameof(ProductMovementsController.GetMovements), PolicyNames.CanRead)]
    [InlineData(nameof(ProductMovementsController.RegisterMovement), PolicyNames.CanOperate)]
    public void Action_RequiresItsAuthorizationPolicy(string actionName, string expectedPolicy)
    {
        var authorization = typeof(ProductMovementsController).GetMethod(actionName)!.GetCustomAttribute<AuthorizeAttribute>();
        authorization.ShouldNotBeNull().Policy.ShouldBe(expectedPolicy);
    }

    [Fact]
    public void Controller_IsRoutedUnderTheProductMovements()
    {
        var route = typeof(ProductMovementsController).GetCustomAttribute<RouteAttribute>();
        route.ShouldNotBeNull().Template.ShouldBe("api/v1/products/{id:int}/movements");
    }

    [Fact]
    public async Task GetMovements_SendsQueryWithIdAndParametersAndReturnsOkWithPage()
    {
        var parameters = new MovementQueryParameters { Type = MovementType.In, From = new DateTimeOffset(2026, 10, 1, 0, 0, 0, TimeSpan.Zero) };
        var expectedPage = new PagedResponse<MovementResponse>([Movement], parameters, 1);
        sender.Send(new GetMovementsQuery(ProductId, parameters), Arg.Any<CancellationToken>()).Returns(expectedPage);
        var response = await controller.GetMovements(ProductId, parameters, CancellationToken.None);
        response.Result.ShouldBeOfType<OkObjectResult>().Value.ShouldBeSameAs(expectedPage);
    }

    [Fact]
    public async Task RegisterMovement_SendsCommandWithIdAndRequestAndReturnsCreatedAtGetMovements()
    {
        var request = new RegisterMovementRequest(MovementType.In, 10, null);
        sender.Send(new RegisterMovementCommand(ProductId, request), Arg.Any<CancellationToken>()).Returns(Movement);
        var response = await controller.RegisterMovement(ProductId, request, CancellationToken.None);
        response.Result.ShouldBeOfType<CreatedAtActionResult>().ShouldSatisfyAllConditions(
            created => created.ActionName.ShouldBe(nameof(ProductMovementsController.GetMovements)),
            created => created.ControllerName.ShouldBeNull(),
            created => created.RouteValues.ShouldNotBeNull()["id"].ShouldBe(ProductId),
            created => created.Value.ShouldBe(Movement));
    }
}
