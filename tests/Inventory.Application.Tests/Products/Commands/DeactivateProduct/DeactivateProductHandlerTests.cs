using Inventory.Application.Common.Exceptions;
using Inventory.Application.Common.Interfaces;
using Inventory.Application.Products.Commands.DeactivateProduct;
using Inventory.Application.Products.Dtos;
using Inventory.Application.Products.Ports;
using Inventory.Application.Tests.Common;
using Inventory.Domain.Areas;
using Inventory.Domain.Categories;
using Inventory.Domain.Common;
using Inventory.Domain.Products;
using NSubstitute;

namespace Inventory.Application.Tests.Products.Commands.DeactivateProduct;

public sealed class DeactivateProductHandlerTests
{
    private const int ProductId = 21;
    private const int CategoryId = 7;
    private const int Stock = 12;
    private const string CurrentUserSubject = "supervisor-1";
    private const string Notes = "Supplier recall";
    private static readonly DateTime CreationTime = new(2026, 10, 27, 9, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime CurrentTime = new(2026, 10, 27, 11, 30, 0, DateTimeKind.Utc);
    private static readonly DeactivateProductCommand Command = new(ProductId, new DeactivateProductRequest(DeactivationReason.Other, Notes));

    private readonly IProductWriteRepository productWriteRepository = Substitute.For<IProductWriteRepository>();
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly Product product = CreateActiveProductWithStock();
    private readonly DeactivateProductHandler handler;

    public DeactivateProductHandlerTests()
    {
        var clock = Substitute.For<IClock>();
        clock.UtcNow.Returns(CurrentTime);
        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.Subject.Returns(CurrentUserSubject);
        productWriteRepository.GetByIdForUpdateAsync(ProductId, Arg.Any<CancellationToken>()).Returns(product);
        productWriteRepository.SaveDeactivationAsync(product, Arg.Any<CancellationToken>()).Returns(true);
        handler = new DeactivateProductHandler(productWriteRepository, unitOfWork, clock, currentUser);
    }

    [Fact]
    public async Task Handle_WithActiveProduct_SavesDeactivationWithReasonNotesCurrentTimeAndUserKeepingStock()
    {
        await handler.Handle(Command, CancellationToken.None);
        await productWriteRepository.Received(1).SaveDeactivationAsync(
            Arg.Is<Product>(deactivated => deactivated == product && !deactivated.IsActive && deactivated.DeactivationReason == DeactivationReason.Other
                && deactivated.DeactivationNotes == Notes && deactivated.DeactivatedAt == CurrentTime && deactivated.DeactivatedBy == CurrentUserSubject
                && deactivated.Stock == Stock),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithActiveProduct_LocksProductInsideTransactionAndCommitsAfterSave()
    {
        await handler.Handle(Command, CancellationToken.None);
        Received.InOrder(() =>
        {
            unitOfWork.BeginTransactionAsync(Arg.Any<CancellationToken>());
            productWriteRepository.GetByIdForUpdateAsync(ProductId, Arg.Any<CancellationToken>());
            productWriteRepository.SaveDeactivationAsync(product, Arg.Any<CancellationToken>());
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
    public async Task Handle_WhenProductIsInactive_NeitherSavesNorCommits()
    {
        product.Deactivate(new ProductDeactivation(DeactivationReason.Expired, null, CreationTime, CurrentUserSubject));
        await Should.ThrowAsync<DomainException>(handler.Handle(Command, CancellationToken.None).AsTask());
        await productWriteRepository.DidNotReceive().SaveDeactivationAsync(Arg.Any<Product>(), Arg.Any<CancellationToken>());
        await unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenConditionalUpdateAffectsNoRows_ThrowsProductInactiveWithProductIdAndDoesNotCommit()
    {
        productWriteRepository.SaveDeactivationAsync(product, Arg.Any<CancellationToken>()).Returns(false);
        var exception = await Should.ThrowAsync<DomainException>(handler.Handle(Command, CancellationToken.None).AsTask());
        exception.Code.ShouldBe(ErrorCode.ProductInactive);
        exception.MessageArguments.ShouldBe([ProductId]);
        await unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    private static Product CreateActiveProductWithStock()
    {
        var area = Area.Create("FOOD", "Food", CreationTime);
        var category = PersistedIdentity.Assign(Category.Create(area, "Dairy", CreationTime), CategoryId);
        var product = PersistedIdentity.Assign(Product.Create(category, "MILK-001", new ProductDetails("Whole milk", null, 4), CreationTime), ProductId);
        typeof(Product).GetProperty(nameof(Product.Stock))!.SetValue(product, Stock);

        return product;
    }
}
