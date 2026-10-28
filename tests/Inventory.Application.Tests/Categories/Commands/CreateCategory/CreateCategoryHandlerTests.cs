using Inventory.Application.Areas.Ports;
using Inventory.Application.Categories.Commands.CreateCategory;
using Inventory.Application.Categories.Dtos;
using Inventory.Application.Categories.Ports;
using Inventory.Application.Common.Exceptions;
using Inventory.Application.Common.Interfaces;
using Inventory.Application.Tests.Common;
using Inventory.Domain.Areas;
using Inventory.Domain.Categories;
using Inventory.Domain.Common;
using NSubstitute;

namespace Inventory.Application.Tests.Categories.Commands.CreateCategory;

public sealed class CreateCategoryHandlerTests
{
    private const int AreaId = 3;
    private const string AreaCode = "WH";
    private const string Name = "Dairy";
    private static readonly DateTime CurrentTime = new(2026, 10, 27, 9, 0, 0, DateTimeKind.Utc);
    private static readonly CreateCategoryCommand Command = new(new CreateCategoryRequest(AreaId, Name));

    private readonly ICategoryWriteRepository categoryWriteRepository = Substitute.For<ICategoryWriteRepository>();
    private readonly IAreaWriteRepository areaWriteRepository = Substitute.For<IAreaWriteRepository>();
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly CreateCategoryHandler handler;

    public CreateCategoryHandlerTests()
    {
        var clock = Substitute.For<IClock>();
        clock.UtcNow.Returns(CurrentTime);
        areaWriteRepository.GetByIdForUpdateAsync(AreaId, Arg.Any<CancellationToken>()).Returns(CreateActiveArea());
        categoryWriteRepository.InsertAsync(Arg.Any<Category>(), Arg.Any<CancellationToken>()).Returns(callInfo => callInfo.Arg<Category>());
        handler = new CreateCategoryHandler(categoryWriteRepository, areaWriteRepository, unitOfWork, clock);
    }

    [Fact]
    public async Task Handle_WithActiveAreaAndAvailableName_InsertsActiveCategoryInAreaCreatedAtCurrentTime()
    {
        await handler.Handle(Command, CancellationToken.None);
        await categoryWriteRepository.Received(1).InsertAsync(
            Arg.Is<Category>(category => category.AreaId == AreaId && category.Name == Name && category.IsActive && category.CreatedAt == CurrentTime),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithActiveAreaAndAvailableName_ReturnsResponseOfPersistedCategoryWithAreaCode()
    {
        var persistedCategory = PersistedIdentity.Assign(Category.Create(CreateActiveArea(), "Persisted dairy", CurrentTime), 15);
        categoryWriteRepository.InsertAsync(Arg.Any<Category>(), Arg.Any<CancellationToken>()).Returns(persistedCategory);
        var response = await handler.Handle(Command, CancellationToken.None);
        response.ShouldBe(new CategoryResponse(15, AreaId, AreaCode, "Persisted dairy", true));
    }

    [Fact]
    public async Task Handle_WithActiveAreaAndAvailableName_LocksAreaInsideTransactionAndCommitsAfterInsert()
    {
        await handler.Handle(Command, CancellationToken.None);
        Received.InOrder(() =>
        {
            unitOfWork.BeginTransactionAsync(Arg.Any<CancellationToken>());
            areaWriteRepository.GetByIdForUpdateAsync(AreaId, Arg.Any<CancellationToken>());
            categoryWriteRepository.ExistsOtherActiveCategoryWithNameAsync(Arg.Any<Category>(), Arg.Any<CancellationToken>());
            categoryWriteRepository.InsertAsync(Arg.Any<Category>(), Arg.Any<CancellationToken>());
            unitOfWork.CommitAsync(Arg.Any<CancellationToken>());
        });
    }

    [Fact]
    public async Task Handle_WhenAreaDoesNotExist_ThrowsReferencedAreaInvalidWithAreaId()
    {
        areaWriteRepository.GetByIdForUpdateAsync(AreaId, Arg.Any<CancellationToken>()).Returns((Area?)null);
        var exception = await Should.ThrowAsync<InvalidReferenceException>(handler.Handle(Command, CancellationToken.None).AsTask());
        exception.Code.ShouldBe(ErrorCode.ReferencedAreaInvalid);
        exception.MessageArguments.ShouldBe([AreaId]);
    }

    [Fact]
    public async Task Handle_WhenAreaIsInactive_ThrowsReferencedAreaInvalid()
    {
        areaWriteRepository.GetByIdForUpdateAsync(AreaId, Arg.Any<CancellationToken>()).Returns(CreateInactiveArea());
        var exception = await Should.ThrowAsync<DomainException>(handler.Handle(Command, CancellationToken.None).AsTask());
        exception.Code.ShouldBe(ErrorCode.ReferencedAreaInvalid);
    }

    [Fact]
    public async Task Handle_WhenNameIsUsedByActiveCategory_ThrowsCategoryNameAlreadyExistsWithName()
    {
        categoryWriteRepository.ExistsOtherActiveCategoryWithNameAsync(Arg.Any<Category>(), Arg.Any<CancellationToken>()).Returns(true);
        var exception = await Should.ThrowAsync<DomainException>(handler.Handle(Command, CancellationToken.None).AsTask());
        exception.Code.ShouldBe(ErrorCode.CategoryNameAlreadyExists);
        exception.MessageArguments.ShouldBe([Name]);
    }

    [Fact]
    public async Task Handle_WhenNameIsUsedByActiveCategory_NeitherInsertsNorCommits()
    {
        categoryWriteRepository.ExistsOtherActiveCategoryWithNameAsync(Arg.Any<Category>(), Arg.Any<CancellationToken>()).Returns(true);
        await Should.ThrowAsync<DomainException>(handler.Handle(Command, CancellationToken.None).AsTask());
        await categoryWriteRepository.DidNotReceive().InsertAsync(Arg.Any<Category>(), Arg.Any<CancellationToken>());
        await unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    private static Area CreateActiveArea()
    {
        return PersistedIdentity.Assign(Area.Create(AreaCode, "Warehouse", CurrentTime), AreaId);
    }

    private static Area CreateInactiveArea()
    {
        var area = CreateActiveArea();
        area.Deactivate(0);

        return area;
    }
}
