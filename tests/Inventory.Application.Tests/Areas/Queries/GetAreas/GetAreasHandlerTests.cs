using Inventory.Application.Areas.Dtos;
using Inventory.Application.Areas.Ports;
using Inventory.Application.Areas.Queries.GetAreas;
using Inventory.Application.Common.Pagination;
using NSubstitute;

namespace Inventory.Application.Tests.Areas.Queries.GetAreas;

public sealed class GetAreasHandlerTests
{
    private readonly IAreaReadRepository areaReadRepository = Substitute.For<IAreaReadRepository>();
    private readonly GetAreasHandler handler;

    public GetAreasHandlerTests()
    {
        handler = new GetAreasHandler(areaReadRepository);
    }

    [Fact]
    public async Task Handle_ReturnsThePageReadWithTheQueryParameters()
    {
        var parameters = new AreaQueryParameters { Page = 2, PageSize = 5, Status = StatusFilter.All };
        var expectedPage = new PagedResponse<AreaResponse>([new AreaResponse(7, "WH", "Warehouse", false)], parameters, 6);
        areaReadRepository.GetPageAsync(parameters, Arg.Any<CancellationToken>()).Returns(expectedPage);
        var page = await handler.Handle(new GetAreasQuery(parameters), CancellationToken.None);
        page.ShouldBeSameAs(expectedPage);
    }
}
