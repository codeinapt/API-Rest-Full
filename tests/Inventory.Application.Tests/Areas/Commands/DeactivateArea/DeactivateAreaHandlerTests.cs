using Inventory.Application.Areas.Commands.DeactivateArea;
using Inventory.Application.Areas.Ports;
using Inventory.Application.Common.Exceptions;
using Inventory.Application.Common.Interfaces;
using Inventory.Domain.Areas;
using Inventory.Domain.Common;
using NSubstitute;

namespace Inventory.Application.Tests.Areas.Commands.DeactivateArea;

public sealed class DeactivateAreaHandlerTests
{
    private const int AreaId = 7;
    private static readonly DateTime CreationTime = new(2026, 10, 27, 9, 0, 0, DateTimeKind.Utc);
    private static readonly DeactivateAreaCommand Command = new(AreaId);

    private readonly IAreaWriteRepository areaWriteRepository = Substitute.For<IAreaWriteRepository>();
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly DeactivateAreaHandler handler;

    public DeactivateAreaHandlerTests()
    {
        handler = new DeactivateAreaHandler(areaWriteRepository, unitOfWork);
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
    public async Task Handle_WhenAreaHasActiveCategories_ThrowsAreaHasActiveCategories()
    {
        areaWriteRepository.GetByIdForUpdateAsync(AreaId, Arg.Any<CancellationToken>()).Returns(CreateActiveArea());
        areaWriteRepository.CountActiveCategoriesAsync(AreaId, Arg.Any<CancellationToken>()).Returns(2);
        var exception = await Should.ThrowAsync<DomainException>(handler.Handle(Command, CancellationToken.None).AsTask());
        exception.Code.ShouldBe(ErrorCode.AreaHasActiveCategories);
    }

    [Fact]
    public async Task Handle_WhenAreaHasActiveCategories_NeitherUpdatesNorCommits()
    {
        areaWriteRepository.GetByIdForUpdateAsync(AreaId, Arg.Any<CancellationToken>()).Returns(CreateActiveArea());
        areaWriteRepository.CountActiveCategoriesAsync(AreaId, Arg.Any<CancellationToken>()).Returns(1);
        await Should.ThrowAsync<DomainException>(handler.Handle(Command, CancellationToken.None).AsTask());
        await areaWriteRepository.DidNotReceive().UpdateAsync(Arg.Any<Area>(), Arg.Any<CancellationToken>());
        await unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenAreaHasNoActiveCategories_UpdatesAreaAsInactive()
    {
        var area = CreateActiveArea();
        areaWriteRepository.GetByIdForUpdateAsync(AreaId, Arg.Any<CancellationToken>()).Returns(area);
        await handler.Handle(Command, CancellationToken.None);
        await areaWriteRepository.Received(1).UpdateAsync(
            Arg.Is<Area>(updated => updated == area && !updated.IsActive),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenAreaHasNoActiveCategories_CommitsTransactionAfterUpdate()
    {
        areaWriteRepository.GetByIdForUpdateAsync(AreaId, Arg.Any<CancellationToken>()).Returns(CreateActiveArea());
        await handler.Handle(Command, CancellationToken.None);
        Received.InOrder(() =>
        {
            unitOfWork.BeginTransactionAsync(Arg.Any<CancellationToken>());
            areaWriteRepository.GetByIdForUpdateAsync(AreaId, Arg.Any<CancellationToken>());
            areaWriteRepository.CountActiveCategoriesAsync(AreaId, Arg.Any<CancellationToken>());
            areaWriteRepository.UpdateAsync(Arg.Any<Area>(), Arg.Any<CancellationToken>());
            unitOfWork.CommitAsync(Arg.Any<CancellationToken>());
        });
    }

    private static Area CreateActiveArea()
    {
        return Area.Create("WH", "Warehouse", CreationTime);
    }

    private static Area CreateInactiveArea()
    {
        var area = CreateActiveArea();
        area.Deactivate(0);

        return area;
    }
}
