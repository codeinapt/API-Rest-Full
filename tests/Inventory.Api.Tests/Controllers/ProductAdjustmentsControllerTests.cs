using System.Reflection;
using Inventory.Api.Authorization;
using Inventory.Api.Controllers;
using Inventory.Application.Movements.Commands.RegisterAdjustment;
using Inventory.Application.Movements.Dtos;
using Inventory.Domain.Movements;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace Inventory.Api.Tests.Controllers;

public sealed class ProductAdjustmentsControllerTests
{
    private const int ProductId = 21;
    private static readonly MovementResponse Adjustment = new(6, ProductId, MovementType.Adjustment, -2, 10, 8, AdjustmentReason.LossOrTheft, "Broken", 5, new DateTime(2026, 10, 2, 8, 0, 0, DateTimeKind.Utc), "supervisor-sub");

    private readonly ISender sender = Substitute.For<ISender>();
    private readonly ProductAdjustmentsController controller;

    public ProductAdjustmentsControllerTests()
    {
        controller = new ProductAdjustmentsController(sender);
    }

    [Fact]
    public void RegisterAdjustment_RequiresCanSupervisePolicy()
    {
        var authorization = typeof(ProductAdjustmentsController).GetMethod(nameof(ProductAdjustmentsController.RegisterAdjustment))!.GetCustomAttribute<AuthorizeAttribute>();
        authorization.ShouldNotBeNull().Policy.ShouldBe(PolicyNames.CanSupervise);
    }

    [Fact]
    public void Controller_IsRoutedUnderTheProductAdjustments()
    {
        var route = typeof(ProductAdjustmentsController).GetCustomAttribute<RouteAttribute>();
        route.ShouldNotBeNull().Template.ShouldBe("api/v1/products/{id:int}/adjustments");
    }

    [Fact]
    public async Task RegisterAdjustment_SendsCommandWithIdAndRequestAndReturnsCreatedAtProductMovements()
    {
        var request = new RegisterAdjustmentRequest(-2, AdjustmentReason.LossOrTheft, "Broken", 5);
        sender.Send(new RegisterAdjustmentCommand(ProductId, request), Arg.Any<CancellationToken>()).Returns(Adjustment);
        var response = await controller.RegisterAdjustment(ProductId, request, CancellationToken.None);
        response.Result.ShouldBeOfType<CreatedAtActionResult>().ShouldSatisfyAllConditions(
            created => created.ActionName.ShouldBe(nameof(ProductMovementsController.GetMovements)),
            created => created.ControllerName.ShouldBe("ProductMovements"),
            created => created.RouteValues.ShouldNotBeNull()["id"].ShouldBe(ProductId),
            created => created.Value.ShouldBe(Adjustment));
    }
}
