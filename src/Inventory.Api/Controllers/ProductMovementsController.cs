using Inventory.Api.Authorization;
using Inventory.Application.Common.Pagination;
using Inventory.Application.Movements.Commands.RegisterMovement;
using Inventory.Application.Movements.Dtos;
using Inventory.Application.Movements.Queries.GetMovements;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Inventory.Api.Controllers;

[ApiController]
[Route("api/v1/products/{id:int}/movements")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
public sealed class ProductMovementsController(ISender sender) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = PolicyNames.CanRead)]
    [ProducesResponseType<PagedResponse<MovementResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PagedResponse<MovementResponse>>> GetMovements(int id, [FromQuery] MovementQueryParameters parameters, CancellationToken cancellationToken)
    {
        return Ok(await sender.Send(new GetMovementsQuery(id, parameters), cancellationToken));
    }

    [HttpPost]
    [Authorize(Policy = PolicyNames.CanOperate)]
    [ProducesResponseType<MovementResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<MovementResponse>> RegisterMovement(int id, RegisterMovementRequest request, CancellationToken cancellationToken)
    {
        var movement = await sender.Send(new RegisterMovementCommand(id, request), cancellationToken);

        return CreatedAtAction(nameof(GetMovements), new { id }, movement);
    }
}
