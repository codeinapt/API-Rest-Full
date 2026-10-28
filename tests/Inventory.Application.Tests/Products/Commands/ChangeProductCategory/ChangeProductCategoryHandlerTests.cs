using Inventory.Application.Categories.Ports;
using Inventory.Application.Common.Exceptions;
using Inventory.Application.Common.Interfaces;
using Inventory.Application.Products.Commands.ChangeProductCategory;
using Inventory.Application.Products.Dtos;
using Inventory.Application.Products.Ports;
using Inventory.Application.Tests.Common;
using Inventory.Domain.Areas;
using Inventory.Domain.Categories;
using Inventory.Domain.Common;
using Inventory.Domain.Products;
using NSubstitute;

namespace Inventory.Application.Tests.Products.Commands.ChangeProductCategory;

public sealed class ChangeProductCategoryHandlerTests
{
    private const int ProductId = 21;
    private const int CurrentCategoryId = 7;
    private const int TargetCategoryId = 9;
    private const string Sku = "MILK-001";
    private const string Name = "Whole milk";
    private const int MinimumStock = 4;
    private static readonly DateTime CreationTime = new(2026, 10, 27, 9, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime CurrentTime = new(2026, 10, 27, 11, 30, 0, DateTimeKind.Utc);
    private static readonly ChangeProductCategoryCommand Command = new(ProductId, new ChangeProductCategoryRequest(TargetCategoryId));

    private readonly IProductWriteRepository productWriteRepository = Substitute.For<IProductWriteRepository>();
    private readonly ICategoryWriteRepository categoryWriteRepository = Substitute.For<ICategoryWriteRepository>();
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly Product product = CreateActiveProduct();
    private readonly ChangeProductCategoryHandler handler;

    public ChangeProductCategoryHandlerTests()
    {
        var clock = Substitute.For<IClock>();
        clock.UtcNow.Returns(CurrentTime);
        productWriteRepository.GetByIdForUpdateAsync(ProductId, Arg.Any<CancellationToken>()).Returns(product);
        categoryWriteRepository.GetByIdForUpdateAsync(TargetCategoryId, Arg.Any<CancellationToken>()).Returns(CreateActiveCategory(TargetCategoryId));
        handler = new ChangeProductCategoryHandler(productWriteRepository, categoryWriteRepository, unitOfWork, clock);
    }

    [Fact]
    public async Task Handle_WithActiveProductAndActiveTargetCategory_UpdatesCategoryAndCurrentTimeKeepingDetailsAndStock()
    {
        await handler.Handle(Command, CancellationToken.None);
        await productWriteRepository.Received(1).UpdateCategoryAsync(
            Arg.Is<Product>(updated => updated == product && updated.CategoryId == TargetCategoryId && updated.UpdatedAt == CurrentTime
                && updated.Sku == Sku && updated.Name == Name && updated.MinimumStock == MinimumStock && updated.Stock == 0 && updated.IsActive),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithActiveProductAndActiveTargetCategory_LocksProductThenCategoryInsideTransactionAndCommitsAfterUpdate()
    {
        await handler.Handle(Command, CancellationToken.None);
        Received.InOrder(() =>
        {
            unitOfWork.BeginTransactionAsync(Arg.Any<CancellationToken>());
            productWriteRepository.GetByIdForUpdateAsync(ProductId, Arg.Any<CancellationToken>());
            categoryWriteRepository.GetByIdForUpdateAsync(TargetCategoryId, Arg.Any<CancellationToken>());
            productWriteRepository.UpdateCategoryAsync(product, Arg.Any<CancellationToken>());
            unitOfWork.CommitAsync(Arg.Any<CancellationToken>());
        });
    }

    [Fact]
    public async Task Handle_WhenProductDoesNotExist_ThrowsProductNotFoundWithProductId()
    {
        productWriteRepository.GetByIdForUpdateAsync(ProductId, Arg.Any<CancellationToken>()).Returns((Product?)null);
        var exception = await Should.ThrowAsync<NotFoundException>(handler.Handle(Command, CancellationToken.None).AsTask());
        exception.Code.ShouldBe(ErrorCode.ProductNotFound);
        exception.MessageArguments.ShouldBe([ProductId]);
    }

    [Fact]
    public async Task Handle_WhenProductIsInactive_ThrowsProductInactiveWithProductId()
    {
        product.Deactivate(new ProductDeactivation(DeactivationReason.Expired, null, CreationTime, "operator-1"));
        var exception = await Should.ThrowAsync<DomainException>(handler.Handle(Command, CancellationToken.None).AsTask());
        exception.Code.ShouldBe(ErrorCode.ProductInactive);
        exception.MessageArguments.ShouldBe([ProductId]);
    }

    [Fact]
    public async Task Handle_WhenTargetCategoryDoesNotExist_ThrowsReferencedCategoryInvalidWithCategoryId()
    {
        categoryWriteRepository.GetByIdForUpdateAsync(TargetCategoryId, Arg.Any<CancellationToken>()).Returns((Category?)null);
        var exception = await Should.ThrowAsync<InvalidReferenceException>(handler.Handle(Command, CancellationToken.None).AsTask());
        exception.Code.ShouldBe(ErrorCode.ReferencedCategoryInvalid);
        exception.MessageArguments.ShouldBe([TargetCategoryId]);
    }

    [Fact]
    public async Task Handle_WhenTargetCategoryIsInactive_ThrowsReferencedCategoryInvalidWithCategoryId()
    {
        categoryWriteRepository.GetByIdForUpdateAsync(TargetCategoryId, Arg.Any<CancellationToken>()).Returns(CreateInactiveCategory(TargetCategoryId));
        var exception = await Should.ThrowAsync<DomainException>(handler.Handle(Command, CancellationToken.None).AsTask());
        exception.Code.ShouldBe(ErrorCode.ReferencedCategoryInvalid);
        exception.MessageArguments.ShouldBe([TargetCategoryId]);
    }

    [Fact]
    public async Task Handle_WhenTargetCategoryIsInactive_NeitherUpdatesNorCommits()
    {
        categoryWriteRepository.GetByIdForUpdateAsync(TargetCategoryId, Arg.Any<CancellationToken>()).Returns(CreateInactiveCategory(TargetCategoryId));
        await Should.ThrowAsync<DomainException>(handler.Handle(Command, CancellationToken.None).AsTask());
        await productWriteRepository.DidNotReceive().UpdateCategoryAsync(Arg.Any<Product>(), Arg.Any<CancellationToken>());
        await unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenProductIsInactive_NeitherUpdatesNorCommits()
    {
        product.Deactivate(new ProductDeactivation(DeactivationReason.Expired, null, CreationTime, "operator-1"));
        await Should.ThrowAsync<DomainException>(handler.Handle(Command, CancellationToken.None).AsTask());
        await productWriteRepository.DidNotReceive().UpdateCategoryAsync(Arg.Any<Product>(), Arg.Any<CancellationToken>());
        await unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    private static Category CreateActiveCategory(int categoryId)
    {
        var area = Area.Create("FOOD", "Food", CreationTime);

        return PersistedIdentity.Assign(Category.Create(area, "Dairy", CreationTime), categoryId);
    }

    private static Category CreateInactiveCategory(int categoryId)
    {
        var category = CreateActiveCategory(categoryId);
        category.Deactivate(0);

        return category;
    }

    private static Product CreateActiveProduct()
    {
        var product = Product.Create(CreateActiveCategory(CurrentCategoryId), Sku, new ProductDetails(Name, null, MinimumStock), CreationTime);

        return PersistedIdentity.Assign(product, ProductId);
    }
}
