using Inventory.Application.Areas.Ports;
using Inventory.Application.Categories.Commands.UpdateCategory;
using Inventory.Application.Categories.Dtos;
using Inventory.Application.Categories.Ports;
using Inventory.Application.Common.Exceptions;
using Inventory.Application.Common.Interfaces;
using Inventory.Application.Tests.Common;
using Inventory.Domain.Areas;
using Inventory.Domain.Categories;
using Inventory.Domain.Common;
using NSubstitute;

namespace Inventory.Application.Tests.Categories.Commands.UpdateCategory;

public sealed class UpdateCategoryHandlerTests
{
    private const int CategoryId = 15;
    private const int CurrentAreaId = 3;
    private const int TargetAreaId = 8;
    private const string NewName = "Frozen food";
    private static readonly DateTime CreationTime = new(2026, 10, 27, 9, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime CurrentTime = new(2026, 10, 27, 11, 30, 0, DateTimeKind.Utc);
    private static readonly UpdateCategoryCommand Command = new(CategoryId, new UpdateCategoryRequest(TargetAreaId, NewName));

    private readonly ICategoryWriteRepository categoryWriteRepository = Substitute.For<ICategoryWriteRepository>();
    private readonly IAreaWriteRepository areaWriteRepository = Substitute.For<IAreaWriteRepository>();
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly Category category = CreateActiveCategory();
    private readonly UpdateCategoryHandler handler;

    public UpdateCategoryHandlerTests()
    {
        var clock = Substitute.For<IClock>();
        clock.UtcNow.Returns(CurrentTime);
        categoryWriteRepository.GetByIdForUpdateAsync(CategoryId, Arg.Any<CancellationToken>()).Returns(category);
        areaWriteRepository.GetByIdForUpdateAsync(TargetAreaId, Arg.Any<CancellationToken>()).Returns(CreateActiveArea(TargetAreaId, "COLD"));
        handler = new UpdateCategoryHandler(categoryWriteRepository, areaWriteRepository, unitOfWork, clock);
    }

    [Fact]
    public async Task Handle_WithValidChanges_UpdatesCategoryWithTargetAreaNameAndCurrentTime()
    {
        await handler.Handle(Command, CancellationToken.None);
        await categoryWriteRepository.Received(1).UpdateAsync(
            Arg.Is<Category>(updated => updated == category && updated.AreaId == TargetAreaId && updated.Name == NewName && updated.UpdatedAt == CurrentTime),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithValidChanges_ChecksNameUniquenessOfUpdatedCategory()
    {
        await handler.Handle(Command, CancellationToken.None);
        await categoryWriteRepository.Received(1).ExistsOtherActiveCategoryWithNameAsync(
            Arg.Is<Category>(checkedCategory => checkedCategory == category && checkedCategory.Name == NewName),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithValidChanges_LocksCategoryAndAreaInsideTransactionAndCommitsAfterUpdate()
    {
        await handler.Handle(Command, CancellationToken.None);
        Received.InOrder(() =>
        {
            unitOfWork.BeginTransactionAsync(Arg.Any<CancellationToken>());
            categoryWriteRepository.GetByIdForUpdateAsync(CategoryId, Arg.Any<CancellationToken>());
            areaWriteRepository.GetByIdForUpdateAsync(TargetAreaId, Arg.Any<CancellationToken>());
            categoryWriteRepository.ExistsOtherActiveCategoryWithNameAsync(Arg.Any<Category>(), Arg.Any<CancellationToken>());
            categoryWriteRepository.UpdateAsync(Arg.Any<Category>(), Arg.Any<CancellationToken>());
            unitOfWork.CommitAsync(Arg.Any<CancellationToken>());
        });
    }

    [Fact]
    public async Task Handle_WhenCategoryDoesNotExist_ThrowsCategoryNotFoundWithId()
    {
        categoryWriteRepository.GetByIdForUpdateAsync(CategoryId, Arg.Any<CancellationToken>()).Returns((Category?)null);
        var exception = await Should.ThrowAsync<NotFoundException>(handler.Handle(Command, CancellationToken.None).AsTask());
        exception.Code.ShouldBe(ErrorCode.CategoryNotFound);
        exception.MessageArguments.ShouldBe([CategoryId]);
    }

    [Fact]
    public async Task Handle_WhenCategoryIsInactive_ThrowsCategoryInactive()
    {
        category.Deactivate(0);
        var exception = await Should.ThrowAsync<DomainException>(handler.Handle(Command, CancellationToken.None).AsTask());
        exception.Code.ShouldBe(ErrorCode.CategoryInactive);
    }

    [Fact]
    public async Task Handle_WhenTargetAreaDoesNotExist_ThrowsReferencedAreaInvalidWithAreaId()
    {
        areaWriteRepository.GetByIdForUpdateAsync(TargetAreaId, Arg.Any<CancellationToken>()).Returns((Area?)null);
        var exception = await Should.ThrowAsync<InvalidReferenceException>(handler.Handle(Command, CancellationToken.None).AsTask());
        exception.Code.ShouldBe(ErrorCode.ReferencedAreaInvalid);
        exception.MessageArguments.ShouldBe([TargetAreaId]);
    }

    [Fact]
    public async Task Handle_WhenTargetAreaIsInactive_ThrowsReferencedAreaInvalid()
    {
        var inactiveArea = CreateActiveArea(TargetAreaId, "COLD");
        inactiveArea.Deactivate(0);
        areaWriteRepository.GetByIdForUpdateAsync(TargetAreaId, Arg.Any<CancellationToken>()).Returns(inactiveArea);
        var exception = await Should.ThrowAsync<DomainException>(handler.Handle(Command, CancellationToken.None).AsTask());
        exception.Code.ShouldBe(ErrorCode.ReferencedAreaInvalid);
    }

    [Fact]
    public async Task Handle_WhenNameIsUsedByAnotherActiveCategory_ThrowsCategoryNameAlreadyExistsWithName()
    {
        categoryWriteRepository.ExistsOtherActiveCategoryWithNameAsync(Arg.Any<Category>(), Arg.Any<CancellationToken>()).Returns(true);
        var exception = await Should.ThrowAsync<DomainException>(handler.Handle(Command, CancellationToken.None).AsTask());
        exception.Code.ShouldBe(ErrorCode.CategoryNameAlreadyExists);
        exception.MessageArguments.ShouldBe([NewName]);
    }

    [Fact]
    public async Task Handle_WhenNameIsUsedByAnotherActiveCategory_NeitherUpdatesNorCommits()
    {
        categoryWriteRepository.ExistsOtherActiveCategoryWithNameAsync(Arg.Any<Category>(), Arg.Any<CancellationToken>()).Returns(true);
        await Should.ThrowAsync<DomainException>(handler.Handle(Command, CancellationToken.None).AsTask());
        await categoryWriteRepository.DidNotReceive().UpdateAsync(Arg.Any<Category>(), Arg.Any<CancellationToken>());
        await unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    private static Area CreateActiveArea(int areaId, string code)
    {
        return PersistedIdentity.Assign(Area.Create(code, "Warehouse", CreationTime), areaId);
    }

    private static Category CreateActiveCategory()
    {
        var category = Category.Create(CreateActiveArea(CurrentAreaId, "WH"), "Dairy", CreationTime);

        return PersistedIdentity.Assign(category, CategoryId);
    }
}
