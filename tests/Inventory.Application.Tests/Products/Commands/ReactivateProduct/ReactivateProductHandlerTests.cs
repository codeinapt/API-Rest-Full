using Inventory.Application.Categories.Ports;
using Inventory.Application.Common.Authorization;
using Inventory.Application.Common.Exceptions;
using Inventory.Application.Common.Interfaces;
using Inventory.Application.Products.Commands.ReactivateProduct;
using Inventory.Application.Products.Ports;
using Inventory.Application.Tests.Common;
using Inventory.Domain.Areas;
using Inventory.Domain.Categories;
using Inventory.Domain.Common;
using Inventory.Domain.Products;
using NSubstitute;

namespace Inventory.Application.Tests.Products.Commands.ReactivateProduct;

public sealed class ReactivateProductHandlerTests
{
    private const int ProductId = 21;
    private const int CategoryId = 7;
    private const int Stock = 12;
    private const string AreaCode = "FOOD";
    private const string CurrentUserSubject = "supervisor-1";
    private static readonly DateTime CreationTime = new(2026, 10, 27, 9, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime DeactivationTime = new(2026, 10, 27, 10, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime CurrentTime = new(2026, 10, 27, 11, 30, 0, DateTimeKind.Utc);
    private static readonly ReactivateProductCommand Command = new(ProductId);

    private readonly IProductWriteRepository productWriteRepository = Substitute.For<IProductWriteRepository>();
    private readonly ICategoryWriteRepository categoryWriteRepository = Substitute.For<ICategoryWriteRepository>();
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ICurrentUser currentUser = Substitute.For<ICurrentUser>();
    private readonly Category category = CreateActiveCategory();
    private readonly Product product;
    private readonly ReactivateProductHandler handler;

    public ReactivateProductHandlerTests()
    {
        product = CreateInactiveProductWithStock(category);
        var clock = Substitute.For<IClock>();
        clock.UtcNow.Returns(CurrentTime);
        currentUser.Subject.Returns(CurrentUserSubject);
        currentUser.AreaCodes.Returns([AreaCode]);
        productWriteRepository.GetSnapshotWithAreaForUpdateAsync(ProductId, Arg.Any<CancellationToken>()).Returns(new ProductAreaSnapshot(product, AreaCode));
        categoryWriteRepository.GetByIdForUpdateAsync(CategoryId, Arg.Any<CancellationToken>()).Returns(category);
        productWriteRepository.ReactivateAsync(product, Arg.Any<CancellationToken>()).Returns(true);
        handler = new ReactivateProductHandler(productWriteRepository, categoryWriteRepository, new AreaAccessPolicy(currentUser), unitOfWork, clock, currentUser);
    }

    [Fact]
    public async Task Handle_WithInactiveProductInSupervisedArea_ReactivatesWithCurrentTimeAndUserKeepingStockAndDeactivationData()
    {
        await handler.Handle(Command, CancellationToken.None);
        await productWriteRepository.Received(1).ReactivateAsync(
            Arg.Is<Product>(reactivated => reactivated == product && reactivated.IsActive && reactivated.ReactivatedAt == CurrentTime
                && reactivated.ReactivatedBy == CurrentUserSubject && reactivated.Stock == Stock && reactivated.DeactivationReason == DeactivationReason.Expired
                && reactivated.DeactivatedAt == DeactivationTime),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithInactiveProductInSupervisedArea_LocksProductThenCategoryChecksSkuAndCommitsAfterReactivation()
    {
        await handler.Handle(Command, CancellationToken.None);
        Received.InOrder(() =>
        {
            unitOfWork.BeginTransactionAsync(Arg.Any<CancellationToken>());
            productWriteRepository.GetSnapshotWithAreaForUpdateAsync(ProductId, Arg.Any<CancellationToken>());
            categoryWriteRepository.GetByIdForUpdateAsync(CategoryId, Arg.Any<CancellationToken>());
            productWriteRepository.ExistsOtherActiveProductWithSkuAsync(product, Arg.Any<CancellationToken>());
            productWriteRepository.ReactivateAsync(product, Arg.Any<CancellationToken>());
            unitOfWork.CommitAsync(Arg.Any<CancellationToken>());
        });
    }

    [Fact]
    public async Task Handle_WhenProductDoesNotExist_ThrowsProductNotFoundWithProductId()
    {
        productWriteRepository.GetSnapshotWithAreaForUpdateAsync(ProductId, Arg.Any<CancellationToken>()).Returns((ProductAreaSnapshot?)null);
        var exception = await Should.ThrowAsync<NotFoundException>(handler.Handle(Command, CancellationToken.None).AsTask());
        exception.Code.ShouldBe(ErrorCode.ProductNotFound);
        exception.MessageArguments.ShouldBe([ProductId]);
    }

    [Fact]
    public async Task Handle_WhenUserDoesNotSuperviseProductArea_ThrowsAreaAccessDeniedWithAreaCode()
    {
        currentUser.AreaCodes.Returns(["TOOLS"]);
        var exception = await Should.ThrowAsync<ForbiddenException>(handler.Handle(Command, CancellationToken.None).AsTask());
        exception.Code.ShouldBe(ErrorCode.AreaAccessDenied);
        exception.MessageArguments.ShouldBe([AreaCode]);
    }

    [Fact]
    public async Task Handle_WhenUserDoesNotSuperviseProductArea_NeitherLocksCategoryNorReactivatesNorCommits()
    {
        currentUser.AreaCodes.Returns([]);
        await Should.ThrowAsync<ForbiddenException>(handler.Handle(Command, CancellationToken.None).AsTask());
        await categoryWriteRepository.DidNotReceive().GetByIdForUpdateAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());
        await productWriteRepository.DidNotReceive().ReactivateAsync(Arg.Any<Product>(), Arg.Any<CancellationToken>());
        await unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenProductIsActiveAndUserDoesNotSuperviseArea_ThrowsAreaAccessDeniedBeforeCheckingState()
    {
        product.Reactivate(category, DeactivationTime, CurrentUserSubject);
        currentUser.AreaCodes.Returns(["TOOLS"]);
        var exception = await Should.ThrowAsync<ForbiddenException>(handler.Handle(Command, CancellationToken.None).AsTask());
        exception.Code.ShouldBe(ErrorCode.AreaAccessDenied);
    }

    [Fact]
    public async Task Handle_WhenProductIsActive_ThrowsProductAlreadyActiveWithProductId()
    {
        product.Reactivate(category, DeactivationTime, CurrentUserSubject);
        var exception = await Should.ThrowAsync<DomainException>(handler.Handle(Command, CancellationToken.None).AsTask());
        exception.Code.ShouldBe(ErrorCode.ProductAlreadyActive);
        exception.MessageArguments.ShouldBe([ProductId]);
    }

    [Fact]
    public async Task Handle_WhenCategoryIsInactive_ThrowsCategoryInactiveWithCategoryIdAndDoesNotReactivate()
    {
        category.Deactivate(0);
        var exception = await Should.ThrowAsync<DomainException>(handler.Handle(Command, CancellationToken.None).AsTask());
        exception.Code.ShouldBe(ErrorCode.CategoryInactive);
        exception.MessageArguments.ShouldBe([CategoryId]);
        await productWriteRepository.DidNotReceive().ReactivateAsync(Arg.Any<Product>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenSkuIsUsedByAnotherActiveProduct_ThrowsSkuAlreadyExistsWithSkuAndDoesNotReactivate()
    {
        productWriteRepository.ExistsOtherActiveProductWithSkuAsync(product, Arg.Any<CancellationToken>()).Returns(true);
        var exception = await Should.ThrowAsync<DomainException>(handler.Handle(Command, CancellationToken.None).AsTask());
        exception.Code.ShouldBe(ErrorCode.SkuAlreadyExists);
        exception.MessageArguments.ShouldBe([product.Sku]);
        await productWriteRepository.DidNotReceive().ReactivateAsync(Arg.Any<Product>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenConditionalUpdateAffectsNoRows_ThrowsProductAlreadyActiveWithProductIdAndDoesNotCommit()
    {
        productWriteRepository.ReactivateAsync(product, Arg.Any<CancellationToken>()).Returns(false);
        var exception = await Should.ThrowAsync<DomainException>(handler.Handle(Command, CancellationToken.None).AsTask());
        exception.Code.ShouldBe(ErrorCode.ProductAlreadyActive);
        exception.MessageArguments.ShouldBe([ProductId]);
        await unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    private static Category CreateActiveCategory()
    {
        var area = Area.Create(AreaCode, "Food", CreationTime);

        return PersistedIdentity.Assign(Category.Create(area, "Dairy", CreationTime), CategoryId);
    }

    private static Product CreateInactiveProductWithStock(Category category)
    {
        var product = PersistedIdentity.Assign(Product.Create(category, "MILK-001", new ProductDetails("Whole milk", null, 4), CreationTime), ProductId);
        typeof(Product).GetProperty(nameof(Product.Stock))!.SetValue(product, Stock);
        product.Deactivate(new ProductDeactivation(DeactivationReason.Expired, null, DeactivationTime, CurrentUserSubject));

        return product;
    }
}
