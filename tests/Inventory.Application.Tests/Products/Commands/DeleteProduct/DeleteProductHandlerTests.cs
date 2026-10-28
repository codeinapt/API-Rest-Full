using Inventory.Application.Common.Exceptions;
using Inventory.Application.Common.Interfaces;
using Inventory.Application.Products.Commands.DeleteProduct;
using Inventory.Application.Products.Ports;
using Inventory.Application.Tests.Common;
using Inventory.Domain.Areas;
using Inventory.Domain.Categories;
using Inventory.Domain.Common;
using Inventory.Domain.Products;
using NSubstitute;

namespace Inventory.Application.Tests.Products.Commands.DeleteProduct;

public sealed class DeleteProductHandlerTests
{
    private const int ProductId = 21;
    private const int CategoryId = 7;
    private const string CurrentUserSubject = "operator-1";
    private static readonly DateTime CreationTime = new(2026, 10, 27, 9, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime CurrentTime = new(2026, 10, 27, 11, 30, 0, DateTimeKind.Utc);
    private static readonly DeleteProductCommand Command = new(ProductId);

    private readonly IProductWriteRepository productWriteRepository = Substitute.For<IProductWriteRepository>();
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly Product product = CreateActiveProduct();
    private readonly DeleteProductHandler handler;

    public DeleteProductHandlerTests()
    {
        var clock = Substitute.For<IClock>();
        clock.UtcNow.Returns(CurrentTime);
        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.Subject.Returns(CurrentUserSubject);
        productWriteRepository.GetByIdForUpdateAsync(ProductId, Arg.Any<CancellationToken>()).Returns(product);
        productWriteRepository.DeleteAsync(product, Arg.Any<CancellationToken>()).Returns(true);
        handler = new DeleteProductHandler(productWriteRepository, unitOfWork, clock, currentUser);
    }

    [Fact]
    public async Task Handle_WithActiveProductWithoutStock_SavesDiscontinuedDeactivationWithCurrentTimeAndUser()
    {
        await handler.Handle(Command, CancellationToken.None);
        await productWriteRepository.Received(1).DeleteAsync(
            Arg.Is<Product>(deleted => deleted == product && !deleted.IsActive && deleted.DeactivationReason == DeactivationReason.Discontinued
                && deleted.DeactivationNotes == null && deleted.DeactivatedAt == CurrentTime && deleted.DeactivatedBy == CurrentUserSubject),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithActiveProductWithoutStock_LocksProductInsideTransactionAndCommitsAfterDelete()
    {
        await handler.Handle(Command, CancellationToken.None);
        Received.InOrder(() =>
        {
            unitOfWork.BeginTransactionAsync(Arg.Any<CancellationToken>());
            productWriteRepository.GetByIdForUpdateAsync(ProductId, Arg.Any<CancellationToken>());
            productWriteRepository.DeleteAsync(product, Arg.Any<CancellationToken>());
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
        product.Deactivate(new ProductDeactivation(DeactivationReason.Expired, null, CreationTime, CurrentUserSubject));
        var exception = await Should.ThrowAsync<DomainException>(handler.Handle(Command, CancellationToken.None).AsTask());
        exception.Code.ShouldBe(ErrorCode.ProductInactive);
        exception.MessageArguments.ShouldBe([ProductId]);
    }

    [Fact]
    public async Task Handle_WhenProductHasStock_ThrowsProductHasStockWithStock()
    {
        AssignStock(product, 5);
        var exception = await Should.ThrowAsync<DomainException>(handler.Handle(Command, CancellationToken.None).AsTask());
        exception.Code.ShouldBe(ErrorCode.ProductHasStock);
        exception.MessageArguments.ShouldBe([5]);
    }

    [Fact]
    public async Task Handle_WhenProductHasStock_NeitherDeletesNorCommits()
    {
        AssignStock(product, 5);
        await Should.ThrowAsync<DomainException>(handler.Handle(Command, CancellationToken.None).AsTask());
        await productWriteRepository.DidNotReceive().DeleteAsync(Arg.Any<Product>(), Arg.Any<CancellationToken>());
        await unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenConditionalDeleteAffectsNoRows_ThrowsProductHasStockWithStockAndDoesNotCommit()
    {
        productWriteRepository.DeleteAsync(product, Arg.Any<CancellationToken>()).Returns(false);
        var exception = await Should.ThrowAsync<DomainException>(handler.Handle(Command, CancellationToken.None).AsTask());
        exception.Code.ShouldBe(ErrorCode.ProductHasStock);
        exception.MessageArguments.ShouldBe([0]);
        await unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    private static void AssignStock(Product product, int stock)
    {
        typeof(Product).GetProperty(nameof(Product.Stock))!.SetValue(product, stock);
    }

    private static Product CreateActiveProduct()
    {
        var area = Area.Create("FOOD", "Food", CreationTime);
        var category = PersistedIdentity.Assign(Category.Create(area, "Dairy", CreationTime), CategoryId);
        var product = Product.Create(category, "MILK-001", new ProductDetails("Whole milk", null, 4), CreationTime);

        return PersistedIdentity.Assign(product, ProductId);
    }
}
