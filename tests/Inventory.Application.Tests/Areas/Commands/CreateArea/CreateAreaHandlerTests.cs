using Inventory.Application.Areas.Commands.CreateArea;
using Inventory.Application.Areas.Dtos;
using Inventory.Application.Areas.Ports;
using Inventory.Application.Common.Interfaces;
using Inventory.Domain.Areas;
using Inventory.Domain.Common;
using NSubstitute;

namespace Inventory.Application.Tests.Areas.Commands.CreateArea;

public sealed class CreateAreaHandlerTests
{
    private const string Code = "WH";
    private const string Name = "Warehouse";
    private static readonly DateTime CurrentTime = new(2026, 10, 27, 9, 0, 0, DateTimeKind.Utc);
    private static readonly CreateAreaCommand Command = new(new CreateAreaRequest(Code, Name));

    private readonly IAreaWriteRepository areaWriteRepository = Substitute.For<IAreaWriteRepository>();
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly CreateAreaHandler handler;

    public CreateAreaHandlerTests()
    {
        var clock = Substitute.For<IClock>();
        clock.UtcNow.Returns(CurrentTime);
        areaWriteRepository.InsertAsync(Arg.Any<Area>(), Arg.Any<CancellationToken>()).Returns(callInfo => callInfo.Arg<Area>());
        handler = new CreateAreaHandler(areaWriteRepository, unitOfWork, clock);
    }

    [Fact]
    public async Task Handle_WithAvailableCode_InsertsActiveAreaCreatedAtCurrentTime()
    {
        await handler.Handle(Command, CancellationToken.None);
        await areaWriteRepository.Received(1).InsertAsync(
            Arg.Is<Area>(area => area.Code == Code && area.Name == Name && area.IsActive && area.CreatedAt == CurrentTime),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithAvailableCode_ReturnsResponseOfPersistedArea()
    {
        var persistedArea = Area.Create(Code, "Persisted warehouse", CurrentTime);
        areaWriteRepository.InsertAsync(Arg.Any<Area>(), Arg.Any<CancellationToken>()).Returns(persistedArea);
        var response = await handler.Handle(Command, CancellationToken.None);
        response.ShouldBe(new AreaResponse(persistedArea.Id, Code, "Persisted warehouse", true));
    }

    [Fact]
    public async Task Handle_WithAvailableCode_CommitsTransactionAfterInsert()
    {
        await handler.Handle(Command, CancellationToken.None);
        Received.InOrder(() =>
        {
            unitOfWork.BeginTransactionAsync(Arg.Any<CancellationToken>());
            areaWriteRepository.ExistsActiveCodeAsync(Code, Arg.Any<CancellationToken>());
            areaWriteRepository.InsertAsync(Arg.Any<Area>(), Arg.Any<CancellationToken>());
            unitOfWork.CommitAsync(Arg.Any<CancellationToken>());
        });
    }

    [Fact]
    public async Task Handle_WhenCodeIsUsedByActiveArea_ThrowsAreaCodeAlreadyExistsWithCode()
    {
        areaWriteRepository.ExistsActiveCodeAsync(Code, Arg.Any<CancellationToken>()).Returns(true);
        var exception = await Should.ThrowAsync<DomainException>(handler.Handle(Command, CancellationToken.None).AsTask());
        exception.Code.ShouldBe(ErrorCode.AreaCodeAlreadyExists);
        exception.MessageArguments.ShouldBe([Code]);
    }

    [Fact]
    public async Task Handle_WhenCodeIsUsedByActiveArea_NeitherInsertsNorCommits()
    {
        areaWriteRepository.ExistsActiveCodeAsync(Code, Arg.Any<CancellationToken>()).Returns(true);
        await Should.ThrowAsync<DomainException>(handler.Handle(Command, CancellationToken.None).AsTask());
        await areaWriteRepository.DidNotReceive().InsertAsync(Arg.Any<Area>(), Arg.Any<CancellationToken>());
        await unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }
}
