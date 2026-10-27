using Inventory.Api.Authorization;
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

namespace Inventory.Api.Controllers;

[ApiController]
[Route("api/v1/areas")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
public sealed class AreasController(ISender sender) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = PolicyNames.CanRead)]
    [ProducesResponseType<PagedResponse<AreaResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResponse<AreaResponse>>> GetAreas([FromQuery] AreaQueryParameters parameters, CancellationToken cancellationToken)
    {
        return Ok(await sender.Send(new GetAreasQuery(parameters), cancellationToken));
    }

    [HttpGet("{id:int}")]
    [Authorize(Policy = PolicyNames.CanRead)]
    [ProducesResponseType<AreaResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AreaResponse>> GetAreaById(int id, CancellationToken cancellationToken)
    {
        return Ok(await sender.Send(new GetAreaByIdQuery(id), cancellationToken));
    }

    [HttpPost]
    [Authorize(Policy = PolicyNames.CanAdminister)]
    [ProducesResponseType<AreaResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AreaResponse>> CreateArea(CreateAreaRequest request, CancellationToken cancellationToken)
    {
        var area = await sender.Send(new CreateAreaCommand(request), cancellationToken);

        return CreatedAtAction(nameof(GetAreaById), new { id = area.Id }, area);
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = PolicyNames.CanAdminister)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateArea(int id, UpdateAreaRequest request, CancellationToken cancellationToken)
    {
        await sender.Send(new UpdateAreaCommand(id, request), cancellationToken);

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = PolicyNames.CanAdminister)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeactivateArea(int id, CancellationToken cancellationToken)
    {
        await sender.Send(new DeactivateAreaCommand(id), cancellationToken);

        return NoContent();
    }
}
