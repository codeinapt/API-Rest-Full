using Inventory.Application.Areas.Dtos;
using Inventory.Application.Areas.Ports;
using Inventory.Application.Areas.Queries.GetAreaById;
using Inventory.Application.Common.Exceptions;
using Inventory.Domain.Common;
using NSubstitute;

namespace Inventory.Application.Tests.Areas.Queries.GetAreaById;

public sealed class GetAreaByIdHandlerTests
{
    private const int AreaId = 7;
    private static readonly GetAreaByIdQuery Query = new(AreaId);

    private readonly IAreaReadRepository areaReadRepository = Substitute.For<IAreaReadRepository>();
    private readonly GetAreaByIdHandler handler;

    public GetAreaByIdHandlerTests()
    {
        handler = new GetAreaByIdHandler(areaReadRepository);
    }

    [Fact]
    public async Task Handle_WhenAreaExists_ReturnsIt()
    {
        var expectedArea = new AreaResponse(AreaId, "WH", "Warehouse", true);
        areaReadRepository.GetByIdAsync(AreaId, Arg.Any<CancellationToken>()).Returns(expectedArea);
        var area = await handler.Handle(Query, CancellationToken.None);
        area.ShouldBe(expectedArea);
    }

    [Fact]
    public async Task Handle_WhenAreaIsInactive_StillReturnsIt()
    {
        var inactiveArea = new AreaResponse(AreaId, "WH", "Warehouse", false);
        areaReadRepository.GetByIdAsync(AreaId, Arg.Any<CancellationToken>()).Returns(inactiveArea);
        var area = await handler.Handle(Query, CancellationToken.None);
        area.IsActive.ShouldBeFalse();
    }

    [Fact]
    public async Task Handle_WhenAreaDoesNotExist_ThrowsAreaNotFoundWithId()
    {
        areaReadRepository.GetByIdAsync(AreaId, Arg.Any<CancellationToken>()).Returns((AreaResponse?)null);
        var exception = await Should.ThrowAsync<NotFoundException>(handler.Handle(Query, CancellationToken.None).AsTask());
        exception.Code.ShouldBe(ErrorCode.AreaNotFound);
        exception.MessageArguments.ShouldBe([AreaId]);
    }
}
