using Inventory.Application.Categories.Commands.DeactivateCategory;
using Inventory.Application.Categories.Ports;
using Inventory.Application.Common.Exceptions;
using Inventory.Application.Common.Interfaces;
using Inventory.Domain.Areas;
using Inventory.Domain.Categories;
using Inventory.Domain.Common;
using NSubstitute;

namespace Inventory.Application.Tests.Categories.Commands.DeactivateCategory;

public sealed class DeactivateCategoryHandlerTests
{
    private const int CategoryId = 15;
    private static readonly DateTime CreationTime = new(2026, 10, 27, 9, 0, 0, DateTimeKind.Utc);
    private static readonly DeactivateCategoryCommand Command = new(CategoryId);

    private readonly ICategoryWriteRepository categoryWriteRepository = Substitute.For<ICategoryWriteRepository>();
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly DeactivateCategoryHandler handler;

    public DeactivateCategoryHandlerTests()
    {
        handler = new DeactivateCategoryHandler(categoryWriteRepository, unitOfWork);
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
        categoryWriteRepository.GetByIdForUpdateAsync(CategoryId, Arg.Any<CancellationToken>()).Returns(CreateInactiveCategory());
        var exception = await Should.ThrowAsync<DomainException>(handler.Handle(Command, CancellationToken.None).AsTask());
        exception.Code.ShouldBe(ErrorCode.CategoryInactive);
    }

    [Fact]
    public async Task Handle_WhenCategoryHasActiveProducts_ThrowsCategoryHasActiveProducts()
    {
        categoryWriteRepository.GetByIdForUpdateAsync(CategoryId, Arg.Any<CancellationToken>()).Returns(CreateActiveCategory());
        categoryWriteRepository.CountActiveProductsAsync(CategoryId, Arg.Any<CancellationToken>()).Returns(2);
        var exception = await Should.ThrowAsync<DomainException>(handler.Handle(Command, CancellationToken.None).AsTask());
        exception.Code.ShouldBe(ErrorCode.CategoryHasActiveProducts);
    }

    [Fact]
    public async Task Handle_WhenCategoryHasActiveProducts_NeitherUpdatesNorCommits()
    {
        categoryWriteRepository.GetByIdForUpdateAsync(CategoryId, Arg.Any<CancellationToken>()).Returns(CreateActiveCategory());
        categoryWriteRepository.CountActiveProductsAsync(CategoryId, Arg.Any<CancellationToken>()).Returns(1);
        await Should.ThrowAsync<DomainException>(handler.Handle(Command, CancellationToken.None).AsTask());
        await categoryWriteRepository.DidNotReceive().UpdateAsync(Arg.Any<Category>(), Arg.Any<CancellationToken>());
        await unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenCategoryHasNoActiveProducts_UpdatesCategoryAsInactive()
    {
        var category = CreateActiveCategory();
        categoryWriteRepository.GetByIdForUpdateAsync(CategoryId, Arg.Any<CancellationToken>()).Returns(category);
        await handler.Handle(Command, CancellationToken.None);
        await categoryWriteRepository.Received(1).UpdateAsync(
            Arg.Is<Category>(updated => updated == category && !updated.IsActive),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenCategoryHasNoActiveProducts_CommitsTransactionAfterUpdate()
    {
        categoryWriteRepository.GetByIdForUpdateAsync(CategoryId, Arg.Any<CancellationToken>()).Returns(CreateActiveCategory());
        await handler.Handle(Command, CancellationToken.None);
        Received.InOrder(() =>
        {
            unitOfWork.BeginTransactionAsync(Arg.Any<CancellationToken>());
            categoryWriteRepository.GetByIdForUpdateAsync(CategoryId, Arg.Any<CancellationToken>());
            categoryWriteRepository.CountActiveProductsAsync(CategoryId, Arg.Any<CancellationToken>());
            categoryWriteRepository.UpdateAsync(Arg.Any<Category>(), Arg.Any<CancellationToken>());
            unitOfWork.CommitAsync(Arg.Any<CancellationToken>());
        });
    }

    private static Category CreateActiveCategory()
    {
        return Category.Create(Area.Create("WH", "Warehouse", CreationTime), "Dairy", CreationTime);
    }

    private static Category CreateInactiveCategory()
    {
        var category = CreateActiveCategory();
        category.Deactivate(0);

        return category;
    }
}
