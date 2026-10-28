using Inventory.Api.Authorization;
using Inventory.Application.Movements.Commands.RegisterAdjustment;
using Inventory.Application.Movements.Dtos;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Inventory.Api.Controllers;

[ApiController]
[Route("api/v1/products/{id:int}/adjustments")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
public sealed class ProductAdjustmentsController(ISender sender) : ControllerBase
{
    private const string ProductMovementsControllerName = "ProductMovements";

    [HttpPost]
    [Authorize(Policy = PolicyNames.CanSupervise)]
    [ProducesResponseType<MovementResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<MovementResponse>> RegisterAdjustment(int id, RegisterAdjustmentRequest request, CancellationToken cancellationToken)
    {
        var adjustment = await sender.Send(new RegisterAdjustmentCommand(id, request), cancellationToken);

        return CreatedAtAction(nameof(ProductMovementsController.GetMovements), ProductMovementsControllerName, new { id }, adjustment);
    }
}
