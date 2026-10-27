using Inventory.Application.Areas.Ports;
using Inventory.Application.Categories.Ports;
using Inventory.Application.Common.Exceptions;
using Inventory.Application.Common.Interfaces;
using Inventory.Application.Products.Commands.CreateProduct;
using Inventory.Application.Products.Dtos;
using Inventory.Application.Products.Ports;
using Inventory.Application.Tests.Common;
using Inventory.Domain.Areas;
using Inventory.Domain.Categories;
using Inventory.Domain.Common;
using Inventory.Domain.Products;
using NSubstitute;

namespace Inventory.Application.Tests.Products.Commands.CreateProduct;

public sealed class CreateProductHandlerTests
{
    private const int AreaId = 3;
    private const string AreaCode = "FOOD";
    private const int CategoryId = 7;
    private const string CategoryName = "Dairy";
    private const string Sku = "MILK-001";
    private const string Name = "Whole milk";
    private const string Description = "One litre bottle";
    private const int MinimumStock = 5;
    private static readonly DateTime CurrentTime = new(2026, 10, 27, 9, 0, 0, DateTimeKind.Utc);
    private static readonly CreateProductCommand Command = new(new CreateProductRequest(Sku, Name, Description, CategoryId, MinimumStock));

    private readonly IProductWriteRepository productWriteRepository = Substitute.For<IProductWriteRepository>();
    private readonly ICategoryWriteRepository categoryWriteRepository = Substitute.For<ICategoryWriteRepository>();
    private readonly IAreaWriteRepository areaWriteRepository = Substitute.For<IAreaWriteRepository>();
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly CreateProductHandler handler;

    public CreateProductHandlerTests()
    {
        var clock = Substitute.For<IClock>();
        clock.UtcNow.Returns(CurrentTime);
        categoryWriteRepository.GetByIdForUpdateAsync(CategoryId, Arg.Any<CancellationToken>()).Returns(CreateActiveCategory());
        areaWriteRepository.GetByIdForUpdateAsync(AreaId, Arg.Any<CancellationToken>()).Returns(CreateActiveArea());
        productWriteRepository.InsertAsync(Arg.Any<Product>(), Arg.Any<CancellationToken>()).Returns(callInfo => callInfo.Arg<Product>());
        handler = new CreateProductHandler(productWriteRepository, categoryWriteRepository, areaWriteRepository, unitOfWork, clock);
    }

    [Fact]
    public async Task Handle_WithActiveCategoryAndAvailableSku_InsertsActiveProductWithoutStockCreatedAtCurrentTime()
    {
        await handler.Handle(Command, CancellationToken.None);
        await productWriteRepository.Received(1).InsertAsync(
            Arg.Is<Product>(product => product.Sku == Sku && product.Name == Name && product.Description == Description && product.CategoryId == CategoryId
                && product.MinimumStock == MinimumStock && product.Stock == 0 && product.IsActive && product.CreatedAt == CurrentTime),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithActiveCategoryAndAvailableSku_ReturnsResponseOfPersistedProductWithCategoryNameAndAreaCode()
    {
        var persistedProduct = PersistedIdentity.Assign(Product.Create(CreateActiveCategory(), Sku, new ProductDetails(Name, Description, MinimumStock), CurrentTime), 21);
        productWriteRepository.InsertAsync(Arg.Any<Product>(), Arg.Any<CancellationToken>()).Returns(persistedProduct);
        var response = await handler.Handle(Command, CancellationToken.None);
        response.ShouldBe(new ProductResponse(21, Sku, Name, Description, CategoryId, CategoryName, AreaCode, 0, MinimumStock, true, true, null, null, CurrentTime, null));
    }

    [Fact]
    public async Task Handle_WithActiveCategoryAndAvailableSku_LocksCategoryInsideTransactionAndCommitsAfterInsert()
    {
        await handler.Handle(Command, CancellationToken.None);
        Received.InOrder(() =>
        {
            unitOfWork.BeginTransactionAsync(Arg.Any<CancellationToken>());
            categoryWriteRepository.GetByIdForUpdateAsync(CategoryId, Arg.Any<CancellationToken>());
            productWriteRepository.ExistsOtherActiveProductWithSkuAsync(Arg.Any<Product>(), Arg.Any<CancellationToken>());
            productWriteRepository.InsertAsync(Arg.Any<Product>(), Arg.Any<CancellationToken>());
            unitOfWork.CommitAsync(Arg.Any<CancellationToken>());
        });
    }

    [Fact]
    public async Task Handle_WhenCategoryDoesNotExist_ThrowsReferencedCategoryInvalidWithCategoryId()
    {
        categoryWriteRepository.GetByIdForUpdateAsync(CategoryId, Arg.Any<CancellationToken>()).Returns((Category?)null);
        var exception = await Should.ThrowAsync<InvalidReferenceException>(handler.Handle(Command, CancellationToken.None).AsTask());
        exception.Code.ShouldBe(ErrorCode.ReferencedCategoryInvalid);
        exception.MessageArguments.ShouldBe([CategoryId]);
    }

    [Fact]
    public async Task Handle_WhenCategoryIsInactive_ThrowsReferencedCategoryInvalid()
    {
        categoryWriteRepository.GetByIdForUpdateAsync(CategoryId, Arg.Any<CancellationToken>()).Returns(CreateInactiveCategory());
        var exception = await Should.ThrowAsync<DomainException>(handler.Handle(Command, CancellationToken.None).AsTask());
        exception.Code.ShouldBe(ErrorCode.ReferencedCategoryInvalid);
    }

    [Fact]
    public async Task Handle_WhenSkuIsUsedByActiveProduct_ThrowsSkuAlreadyExistsWithSku()
    {
        productWriteRepository.ExistsOtherActiveProductWithSkuAsync(Arg.Any<Product>(), Arg.Any<CancellationToken>()).Returns(true);
        var exception = await Should.ThrowAsync<DomainException>(handler.Handle(Command, CancellationToken.None).AsTask());
        exception.Code.ShouldBe(ErrorCode.SkuAlreadyExists);
        exception.MessageArguments.ShouldBe([Sku]);
    }

    [Fact]
    public async Task Handle_WhenSkuIsUsedByActiveProduct_NeitherInsertsNorCommits()
    {
        productWriteRepository.ExistsOtherActiveProductWithSkuAsync(Arg.Any<Product>(), Arg.Any<CancellationToken>()).Returns(true);
        await Should.ThrowAsync<DomainException>(handler.Handle(Command, CancellationToken.None).AsTask());
        await productWriteRepository.DidNotReceive().InsertAsync(Arg.Any<Product>(), Arg.Any<CancellationToken>());
        await unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    private static Area CreateActiveArea()
    {
        return PersistedIdentity.Assign(Area.Create(AreaCode, "Food", CurrentTime), AreaId);
    }

    private static Category CreateActiveCategory()
    {
        return PersistedIdentity.Assign(Category.Create(CreateActiveArea(), CategoryName, CurrentTime), CategoryId);
    }

    private static Category CreateInactiveCategory()
    {
        var category = CreateActiveCategory();
        category.Deactivate(0);

        return category;
    }
}
