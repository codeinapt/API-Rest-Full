using System.Reflection;
using Inventory.Api.Authorization;
using Inventory.Api.Controllers;
using Inventory.Application.Categories.Commands.CreateCategory;
using Inventory.Application.Categories.Commands.DeactivateCategory;
using Inventory.Application.Categories.Commands.UpdateCategory;
using Inventory.Application.Categories.Dtos;
using Inventory.Application.Categories.Queries.GetCategories;
using Inventory.Application.Categories.Queries.GetCategoryById;
using Inventory.Application.Common.Pagination;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace Inventory.Api.Tests.Controllers;

public sealed class CategoriesControllerTests
{
    private const int CategoryId = 11;
    private const int AreaId = 7;
    private static readonly CategoryResponse Category = new(CategoryId, AreaId, "WH", "Tools", true);

    private readonly ISender sender = Substitute.For<ISender>();
    private readonly CategoriesController controller;

    public CategoriesControllerTests()
    {
        controller = new CategoriesController(sender);
    }

    [Theory]
    [InlineData(nameof(CategoriesController.GetCategories), PolicyNames.CanRead)]
    [InlineData(nameof(CategoriesController.GetCategoryById), PolicyNames.CanRead)]
    [InlineData(nameof(CategoriesController.CreateCategory), PolicyNames.CanAdminister)]
    [InlineData(nameof(CategoriesController.UpdateCategory), PolicyNames.CanAdminister)]
    [InlineData(nameof(CategoriesController.DeactivateCategory), PolicyNames.CanAdminister)]
    public void Action_RequiresItsAuthorizationPolicy(string actionName, string expectedPolicy)
    {
        var authorization = typeof(CategoriesController).GetMethod(actionName)!.GetCustomAttribute<AuthorizeAttribute>();
        authorization.ShouldNotBeNull().Policy.ShouldBe(expectedPolicy);
    }

    [Fact]
    public async Task GetCategories_SendsQueryWithParametersAndReturnsOkWithPage()
    {
        var parameters = new CategoryQueryParameters { AreaId = AreaId, Status = StatusFilter.Inactive };
        var expectedPage = new PagedResponse<CategoryResponse>([Category], parameters, 1);
        sender.Send(Arg.Is<GetCategoriesQuery>(query => query.Parameters == parameters), Arg.Any<CancellationToken>()).Returns(expectedPage);
        var response = await controller.GetCategories(parameters, CancellationToken.None);
        response.Result.ShouldBeOfType<OkObjectResult>().Value.ShouldBeSameAs(expectedPage);
    }

    [Fact]
    public async Task GetCategoryById_SendsQueryWithIdAndReturnsOkWithCategory()
    {
        sender.Send(new GetCategoryByIdQuery(CategoryId), Arg.Any<CancellationToken>()).Returns(Category);
        var response = await controller.GetCategoryById(CategoryId, CancellationToken.None);
        response.Result.ShouldBeOfType<OkObjectResult>().Value.ShouldBe(Category);
    }

    [Fact]
    public async Task CreateCategory_SendsCommandWithRequestAndReturnsCreatedAtGetCategoryById()
    {
        var request = new CreateCategoryRequest(AreaId, "Tools");
        sender.Send(new CreateCategoryCommand(request), Arg.Any<CancellationToken>()).Returns(Category);
        var response = await controller.CreateCategory(request, CancellationToken.None);
        response.Result.ShouldBeOfType<CreatedAtActionResult>().ShouldSatisfyAllConditions(
            created => created.ActionName.ShouldBe(nameof(CategoriesController.GetCategoryById)),
            created => created.RouteValues.ShouldNotBeNull()["id"].ShouldBe(CategoryId),
            created => created.Value.ShouldBe(Category));
    }

    [Fact]
    public async Task UpdateCategory_SendsCommandWithIdAndRequestAndReturnsNoContent()
    {
        var request = new UpdateCategoryRequest(AreaId, "Hand tools");
        var response = await controller.UpdateCategory(CategoryId, request, CancellationToken.None);
        response.ShouldBeOfType<NoContentResult>();
        await sender.Received(1).Send(new UpdateCategoryCommand(CategoryId, request), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeactivateCategory_SendsCommandWithIdAndReturnsNoContent()
    {
        var response = await controller.DeactivateCategory(CategoryId, CancellationToken.None);
        response.ShouldBeOfType<NoContentResult>();
        await sender.Received(1).Send(new DeactivateCategoryCommand(CategoryId), Arg.Any<CancellationToken>());
    }
}
