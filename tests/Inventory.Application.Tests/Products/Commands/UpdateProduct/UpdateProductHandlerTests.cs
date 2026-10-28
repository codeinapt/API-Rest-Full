using Inventory.Application.Common.Exceptions;
using Inventory.Application.Common.Interfaces;
using Inventory.Application.Products.Commands.UpdateProduct;
using Inventory.Application.Products.Dtos;
using Inventory.Application.Products.Ports;
using Inventory.Application.Tests.Common;
using Inventory.Domain.Areas;
using Inventory.Domain.Categories;
using Inventory.Domain.Common;
using Inventory.Domain.Products;
using NSubstitute;

namespace Inventory.Application.Tests.Products.Commands.UpdateProduct;

public sealed class UpdateProductHandlerTests
{
    private const int ProductId = 21;
    private const int CategoryId = 7;
    private const string Sku = "MILK-001";
    private const string NewName = "Skimmed milk";
    private const string NewDescription = "Half litre bottle";
    private const int NewMinimumStock = 12;
    private static readonly DateTime CreationTime = new(2026, 10, 27, 9, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime CurrentTime = new(2026, 10, 27, 11, 30, 0, DateTimeKind.Utc);
    private static readonly UpdateProductCommand Command = new(ProductId, new UpdateProductRequest(NewName, NewDescription, NewMinimumStock));

    private readonly IProductWriteRepository productWriteRepository = Substitute.For<IProductWriteRepository>();
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly Product product = CreateActiveProduct();
    private readonly UpdateProductHandler handler;

    public UpdateProductHandlerTests()
    {
        var clock = Substitute.For<IClock>();
        clock.UtcNow.Returns(CurrentTime);
        productWriteRepository.GetByIdForUpdateAsync(ProductId, Arg.Any<CancellationToken>()).Returns(product);
        handler = new UpdateProductHandler(productWriteRepository, unitOfWork, clock);
    }

    [Fact]
    public async Task Handle_WithActiveProduct_UpdatesDetailsAndCurrentTimeKeepingSkuCategoryAndStock()
    {
        await handler.Handle(Command, CancellationToken.None);
        await productWriteRepository.Received(1).UpdateDetailsAsync(
            Arg.Is<Product>(updated => updated == product && updated.Name == NewName && updated.Description == NewDescription && updated.MinimumStock == NewMinimumStock
                && updated.UpdatedAt == CurrentTime && updated.Sku == Sku && updated.CategoryId == CategoryId && updated.Stock == 0),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithActiveProduct_LocksProductInsideTransactionAndCommitsAfterUpdate()
    {
        await handler.Handle(Command, CancellationToken.None);
        Received.InOrder(() =>
        {
            unitOfWork.BeginTransactionAsync(Arg.Any<CancellationToken>());
            productWriteRepository.GetByIdForUpdateAsync(ProductId, Arg.Any<CancellationToken>());
            productWriteRepository.UpdateDetailsAsync(product, Arg.Any<CancellationToken>());
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
    public async Task Handle_WhenProductIsInactive_NeitherUpdatesNorCommits()
    {
        product.Deactivate(new ProductDeactivation(DeactivationReason.Expired, null, CreationTime, "operator-1"));
        await Should.ThrowAsync<DomainException>(handler.Handle(Command, CancellationToken.None).AsTask());
        await productWriteRepository.DidNotReceive().UpdateDetailsAsync(Arg.Any<Product>(), Arg.Any<CancellationToken>());
        await unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    private static Product CreateActiveProduct()
    {
        var area = Area.Create("FOOD", "Food", CreationTime);
        var category = PersistedIdentity.Assign(Category.Create(area, "Dairy", CreationTime), CategoryId);
        var product = Product.Create(category, Sku, new ProductDetails("Whole milk", null, 0), CreationTime);

        return PersistedIdentity.Assign(product, ProductId);
    }
}
