using System.Linq.Expressions;
using Inventory.Application.Areas.Dtos;
using Inventory.Application.Areas.Ports;
using Inventory.Application.Areas.Queries.GetAreas;
using Inventory.Application.Common.Pagination;
using Inventory.Infrastructure.Persistence.Read.Models;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Infrastructure.Persistence.Read;

internal sealed class AreaReadRepository(InventoryReadDbContext context) : IAreaReadRepository
{
    private static readonly Expression<Func<AreaRow, AreaResponse>> ProjectToResponse =
        area => new AreaResponse(area.Id, area.Code, area.Name, area.IsActive);

    public async Task<AreaResponse?> GetByIdAsync(int areaId, CancellationToken cancellationToken)
    {
        return await context.Areas
            .Where(area => area.Id == areaId)
            .Select(ProjectToResponse)
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<PagedResponse<AreaResponse>> GetPageAsync(AreaQueryParameters parameters, CancellationToken cancellationToken)
    {
        var filteredAreas = FilterByStatus(context.Areas, parameters.Status);
        var totalCount = await filteredAreas.CountAsync(cancellationToken);
        var items = await filteredAreas
            .OrderBy(area => area.Code)
            .ThenBy(area => area.Id)
            .Skip(parameters.CalculateItemsToSkip())
            .Take(parameters.PageSize)
            .Select(ProjectToResponse)
            .ToListAsync(cancellationToken);

        return new PagedResponse<AreaResponse>(items, parameters, totalCount);
    }

    private static IQueryable<AreaRow> FilterByStatus(IQueryable<AreaRow> areas, StatusFilter status)
    {
        return status switch
        {
            StatusFilter.Active => areas.Where(area => area.IsActive),
            StatusFilter.Inactive => areas.Where(area => !area.IsActive),
            _ => areas,
        };
    }
}
