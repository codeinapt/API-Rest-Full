using System.Reflection;
using Inventory.Api.Authorization;
using Inventory.Api.Controllers;
using Inventory.Application.Common.Pagination;
using Inventory.Application.Products.Commands.ChangeProductCategory;
using Inventory.Application.Products.Commands.CreateProduct;
using Inventory.Application.Products.Commands.DeactivateProduct;
using Inventory.Application.Products.Commands.DeleteProduct;
using Inventory.Application.Products.Commands.ReactivateProduct;
using Inventory.Application.Products.Commands.UpdateProduct;
using Inventory.Application.Products.Dtos;
using Inventory.Application.Products.Queries.GetProductById;
using Inventory.Application.Products.Queries.GetProducts;
using Inventory.Domain.Products;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace Inventory.Api.Tests.Controllers;

public sealed class ProductsControllerTests
{
    private const int ProductId = 21;
    private const int CategoryId = 3;
    private static readonly ProductResponse Product = new(ProductId, "DRL-001", "Drill", null, CategoryId, "Tools", "TOOLS", 0, 5, true, true, null, null, new DateTime(2026, 10, 1, 8, 0, 0, DateTimeKind.Utc), null);

    private readonly ISender sender = Substitute.For<ISender>();
    private readonly ProductsController controller;

    public ProductsControllerTests()
    {
        controller = new ProductsController(sender);
    }

    [Theory]
    [InlineData(nameof(ProductsController.GetProducts), PolicyNames.CanRead)]
    [InlineData(nameof(ProductsController.GetProductById), PolicyNames.CanRead)]
    [InlineData(nameof(ProductsController.CreateProduct), PolicyNames.CanOperate)]
    [InlineData(nameof(ProductsController.UpdateProduct), PolicyNames.CanOperate)]
    [InlineData(nameof(ProductsController.ChangeProductCategory), PolicyNames.CanAdminister)]
    [InlineData(nameof(ProductsController.DeleteProduct), PolicyNames.CanOperate)]
    [InlineData(nameof(ProductsController.DeactivateProduct), PolicyNames.CanOperate)]
    [InlineData(nameof(ProductsController.ReactivateProduct), PolicyNames.CanSupervise)]
    public void Action_RequiresItsAuthorizationPolicy(string actionName, string expectedPolicy)
    {
        var authorization = typeof(ProductsController).GetMethod(actionName)!.GetCustomAttribute<AuthorizeAttribute>();
        authorization.ShouldNotBeNull().Policy.ShouldBe(expectedPolicy);
    }

    [Fact]
    public async Task GetProducts_SendsQueryWithParametersAndReturnsOkWithPage()
    {
        var parameters = new ProductQueryParameters { CategoryId = CategoryId, Search = "drill", LowStock = true, SortBy = ProductSortField.Sku, SortDirection = SortDirection.Desc };
        var expectedPage = new PagedResponse<ProductResponse>([Product], parameters, 1);
        sender.Send(Arg.Is<GetProductsQuery>(query => query.Parameters == parameters), Arg.Any<CancellationToken>()).Returns(expectedPage);
        var response = await controller.GetProducts(parameters, CancellationToken.None);
        response.Result.ShouldBeOfType<OkObjectResult>().Value.ShouldBeSameAs(expectedPage);
    }

    [Fact]
    public async Task GetProductById_SendsQueryWithIdAndReturnsOkWithProduct()
    {
        sender.Send(new GetProductByIdQuery(ProductId), Arg.Any<CancellationToken>()).Returns(Product);
        var response = await controller.GetProductById(ProductId, CancellationToken.None);
        response.Result.ShouldBeOfType<OkObjectResult>().Value.ShouldBe(Product);
    }

    [Fact]
    public async Task CreateProduct_SendsCommandWithRequestAndReturnsCreatedAtGetProductById()
    {
        var request = new CreateProductRequest("DRL-001", "Drill", null, CategoryId, 5);
        sender.Send(new CreateProductCommand(request), Arg.Any<CancellationToken>()).Returns(Product);
        var response = await controller.CreateProduct(request, CancellationToken.None);
        response.Result.ShouldBeOfType<CreatedAtActionResult>().ShouldSatisfyAllConditions(
            created => created.ActionName.ShouldBe(nameof(ProductsController.GetProductById)),
            created => created.RouteValues.ShouldNotBeNull()["id"].ShouldBe(ProductId),
            created => created.Value.ShouldBe(Product));
    }

    [Fact]
    public async Task UpdateProduct_SendsCommandWithIdAndRequestAndReturnsNoContent()
    {
        var request = new UpdateProductRequest("Cordless drill", "18 V", 4);
        var response = await controller.UpdateProduct(ProductId, request, CancellationToken.None);
        response.ShouldBeOfType<NoContentResult>();
        await sender.Received(1).Send(new UpdateProductCommand(ProductId, request), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ChangeProductCategory_SendsCommandWithIdAndRequestAndReturnsNoContent()
    {
        var request = new ChangeProductCategoryRequest(CategoryId);
        var response = await controller.ChangeProductCategory(ProductId, request, CancellationToken.None);
        response.ShouldBeOfType<NoContentResult>();
        await sender.Received(1).Send(new ChangeProductCategoryCommand(ProductId, request), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteProduct_SendsCommandWithIdAndReturnsNoContent()
    {
        var response = await controller.DeleteProduct(ProductId, CancellationToken.None);
        response.ShouldBeOfType<NoContentResult>();
        await sender.Received(1).Send(new DeleteProductCommand(ProductId), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeactivateProduct_SendsCommandWithIdAndRequestAndReturnsNoContent()
    {
        var request = new DeactivateProductRequest(DeactivationReason.Expired, null);
        var response = await controller.DeactivateProduct(ProductId, request, CancellationToken.None);
        response.ShouldBeOfType<NoContentResult>();
        await sender.Received(1).Send(new DeactivateProductCommand(ProductId, request), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReactivateProduct_SendsCommandWithIdAndReturnsNoContent()
    {
        var response = await controller.ReactivateProduct(ProductId, CancellationToken.None);
        response.ShouldBeOfType<NoContentResult>();
        await sender.Received(1).Send(new ReactivateProductCommand(ProductId), Arg.Any<CancellationToken>());
    }
}
