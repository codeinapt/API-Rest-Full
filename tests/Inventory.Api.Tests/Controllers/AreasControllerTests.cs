using System.Reflection;
using Inventory.Api.Authorization;
using Inventory.Api.Controllers;
using Inventory.Application.Areas.Commands.CreateArea;
using Inventory.Application.Areas.Commands.DeactivateArea;
using Inventory.Application.Areas.Commands.UpdateArea;
using Inventory.Application.Areas.Dtos;
using Inventory.Application.Areas.Queries.GetAreaById;
using Inventory.Application.Areas.Queries.GetAreas;
using Inventory.Application.Common.Pagination;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace Inventory.Api.Tests.Controllers;

public sealed class AreasControllerTests
{
    private const int AreaId = 7;
    private static readonly AreaResponse Area = new(AreaId, "WH", "Warehouse", true);

    private readonly ISender sender = Substitute.For<ISender>();
    private readonly AreasController controller;

    public AreasControllerTests()
    {
        controller = new AreasController(sender);
    }

    [Theory]
    [InlineData(nameof(AreasController.GetAreas), PolicyNames.CanRead)]
    [InlineData(nameof(AreasController.GetAreaById), PolicyNames.CanRead)]
    [InlineData(nameof(AreasController.CreateArea), PolicyNames.CanAdminister)]
    [InlineData(nameof(AreasController.UpdateArea), PolicyNames.CanAdminister)]
    [InlineData(nameof(AreasController.DeactivateArea), PolicyNames.CanAdminister)]
    public void Action_RequiresItsAuthorizationPolicy(string actionName, string expectedPolicy)
    {
        var authorization = typeof(AreasController).GetMethod(actionName)!.GetCustomAttribute<AuthorizeAttribute>();
        authorization.ShouldNotBeNull().Policy.ShouldBe(expectedPolicy);
    }

    [Fact]
    public async Task GetAreas_SendsQueryWithParametersAndReturnsOkWithPage()
    {
        var parameters = new AreaQueryParameters { Status = StatusFilter.Inactive };
        var expectedPage = new PagedResponse<AreaResponse>([Area], parameters, 1);
        sender.Send(Arg.Is<GetAreasQuery>(query => query.Parameters == parameters), Arg.Any<CancellationToken>()).Returns(expectedPage);
        var response = await controller.GetAreas(parameters, CancellationToken.None);
        response.Result.ShouldBeOfType<OkObjectResult>().Value.ShouldBeSameAs(expectedPage);
    }

    [Fact]
    public async Task GetAreaById_SendsQueryWithIdAndReturnsOkWithArea()
    {
        sender.Send(new GetAreaByIdQuery(AreaId), Arg.Any<CancellationToken>()).Returns(Area);
        var response = await controller.GetAreaById(AreaId, CancellationToken.None);
        response.Result.ShouldBeOfType<OkObjectResult>().Value.ShouldBe(Area);
    }

    [Fact]
    public async Task CreateArea_SendsCommandWithRequestAndReturnsCreatedAtGetAreaById()
    {
        var request = new CreateAreaRequest("WH", "Warehouse");
        sender.Send(new CreateAreaCommand(request), Arg.Any<CancellationToken>()).Returns(Area);
        var response = await controller.CreateArea(request, CancellationToken.None);
        response.Result.ShouldBeOfType<CreatedAtActionResult>().ShouldSatisfyAllConditions(
            created => created.ActionName.ShouldBe(nameof(AreasController.GetAreaById)),
            created => created.RouteValues.ShouldNotBeNull()["id"].ShouldBe(AreaId),
            created => created.Value.ShouldBe(Area));
    }

    [Fact]
    public async Task UpdateArea_SendsCommandWithIdAndRequestAndReturnsNoContent()
    {
        var request = new UpdateAreaRequest("Main warehouse");
        var response = await controller.UpdateArea(AreaId, request, CancellationToken.None);
        response.ShouldBeOfType<NoContentResult>();
        await sender.Received(1).Send(new UpdateAreaCommand(AreaId, request), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeactivateArea_SendsCommandWithIdAndReturnsNoContent()
    {
        var response = await controller.DeactivateArea(AreaId, CancellationToken.None);
        response.ShouldBeOfType<NoContentResult>();
        await sender.Received(1).Send(new DeactivateAreaCommand(AreaId), Arg.Any<CancellationToken>());
    }
}
