using Inventory.Application.Areas.Commands.UpdateArea;
using Inventory.Application.Areas.Dtos;
using Inventory.Application.Areas.Ports;
using Inventory.Application.Common.Exceptions;
using Inventory.Application.Common.Interfaces;
using Inventory.Domain.Areas;
using Inventory.Domain.Common;
using NSubstitute;

namespace Inventory.Application.Tests.Areas.Commands.UpdateArea;

public sealed class UpdateAreaHandlerTests
{
    private const int AreaId = 7;
    private const string NewName = "Store";
    private static readonly DateTime CreationTime = new(2026, 10, 27, 9, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime CurrentTime = new(2026, 10, 28, 9, 0, 0, DateTimeKind.Utc);
    private static readonly UpdateAreaCommand Command = new(AreaId, new UpdateAreaRequest(NewName));

    private readonly IAreaWriteRepository areaWriteRepository = Substitute.For<IAreaWriteRepository>();
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly UpdateAreaHandler handler;

    public UpdateAreaHandlerTests()
    {
        var clock = Substitute.For<IClock>();
        clock.UtcNow.Returns(CurrentTime);
        handler = new UpdateAreaHandler(areaWriteRepository, unitOfWork, clock);
    }

    [Fact]
    public async Task Handle_WhenAreaDoesNotExist_ThrowsAreaNotFoundWithId()
    {
        areaWriteRepository.GetByIdForUpdateAsync(AreaId, Arg.Any<CancellationToken>()).Returns((Area?)null);
        var exception = await Should.ThrowAsync<NotFoundException>(handler.Handle(Command, CancellationToken.None).AsTask());
        exception.Code.ShouldBe(ErrorCode.AreaNotFound);
        exception.MessageArguments.ShouldBe([AreaId]);
    }

    [Fact]
    public async Task Handle_WhenAreaIsInactive_ThrowsAreaInactive()
    {
        areaWriteRepository.GetByIdForUpdateAsync(AreaId, Arg.Any<CancellationToken>()).Returns(CreateInactiveArea());
        var exception = await Should.ThrowAsync<DomainException>(handler.Handle(Command, CancellationToken.None).AsTask());
        exception.Code.ShouldBe(ErrorCode.AreaInactive);
    }

    [Fact]
    public async Task Handle_WhenAreaIsInactive_NeitherUpdatesNorCommits()
    {
        areaWriteRepository.GetByIdForUpdateAsync(AreaId, Arg.Any<CancellationToken>()).Returns(CreateInactiveArea());
        await Should.ThrowAsync<DomainException>(handler.Handle(Command, CancellationToken.None).AsTask());
        await areaWriteRepository.DidNotReceive().UpdateAsync(Arg.Any<Area>(), Arg.Any<CancellationToken>());
        await unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenAreaIsActive_UpdatesAreaWithNewNameAndCurrentTime()
    {
        var area = Area.Create("WH", "Warehouse", CreationTime);
        areaWriteRepository.GetByIdForUpdateAsync(AreaId, Arg.Any<CancellationToken>()).Returns(area);
        await handler.Handle(Command, CancellationToken.None);
        await areaWriteRepository.Received(1).UpdateAsync(
            Arg.Is<Area>(updated => updated == area && updated.Name == NewName && updated.UpdatedAt == CurrentTime),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenAreaIsActive_CommitsTransactionAfterUpdate()
    {
        areaWriteRepository.GetByIdForUpdateAsync(AreaId, Arg.Any<CancellationToken>()).Returns(Area.Create("WH", "Warehouse", CreationTime));
        await handler.Handle(Command, CancellationToken.None);
        Received.InOrder(() =>
        {
            unitOfWork.BeginTransactionAsync(Arg.Any<CancellationToken>());
            areaWriteRepository.GetByIdForUpdateAsync(AreaId, Arg.Any<CancellationToken>());
            areaWriteRepository.UpdateAsync(Arg.Any<Area>(), Arg.Any<CancellationToken>());
            unitOfWork.CommitAsync(Arg.Any<CancellationToken>());
        });
    }

    private static Area CreateInactiveArea()
    {
        var area = Area.Create("WH", "Warehouse", CreationTime);
        area.Deactivate(0);

        return area;
    }
}
