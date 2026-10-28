using Inventory.Api.Authorization;
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
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Inventory.Api.Controllers;

[ApiController]
[Route("api/v1/products")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
public sealed class ProductsController(ISender sender) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = PolicyNames.CanRead)]
    [ProducesResponseType<PagedResponse<ProductResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResponse<ProductResponse>>> GetProducts([FromQuery] ProductQueryParameters parameters, CancellationToken cancellationToken)
    {
        return Ok(await sender.Send(new GetProductsQuery(parameters), cancellationToken));
    }

    [HttpGet("{id:int}")]
    [Authorize(Policy = PolicyNames.CanRead)]
    [ProducesResponseType<ProductResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductResponse>> GetProductById(int id, CancellationToken cancellationToken)
    {
        return Ok(await sender.Send(new GetProductByIdQuery(id), cancellationToken));
    }

    [HttpPost]
    [Authorize(Policy = PolicyNames.CanOperate)]
    [ProducesResponseType<ProductResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ProductResponse>> CreateProduct(CreateProductRequest request, CancellationToken cancellationToken)
    {
        var product = await sender.Send(new CreateProductCommand(request), cancellationToken);

        return CreatedAtAction(nameof(GetProductById), new { id = product.Id }, product);
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = PolicyNames.CanOperate)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateProduct(int id, UpdateProductRequest request, CancellationToken cancellationToken)
    {
        await sender.Send(new UpdateProductCommand(id, request), cancellationToken);

        return NoContent();
    }

    [HttpPut("{id:int}/category")]
    [Authorize(Policy = PolicyNames.CanAdminister)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ChangeProductCategory(int id, ChangeProductCategoryRequest request, CancellationToken cancellationToken)
    {
        await sender.Send(new ChangeProductCategoryCommand(id, request), cancellationToken);

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = PolicyNames.CanOperate)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteProduct(int id, CancellationToken cancellationToken)
    {
        await sender.Send(new DeleteProductCommand(id), cancellationToken);

        return NoContent();
    }

    [HttpPost("{id:int}/deactivation")]
    [Authorize(Policy = PolicyNames.CanOperate)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeactivateProduct(int id, DeactivateProductRequest request, CancellationToken cancellationToken)
    {
        await sender.Send(new DeactivateProductCommand(id, request), cancellationToken);

        return NoContent();
    }

    [HttpPost("{id:int}/reactivation")]
    [Authorize(Policy = PolicyNames.CanSupervise)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ReactivateProduct(int id, CancellationToken cancellationToken)
    {
        await sender.Send(new ReactivateProductCommand(id), cancellationToken);

        return NoContent();
    }
}
