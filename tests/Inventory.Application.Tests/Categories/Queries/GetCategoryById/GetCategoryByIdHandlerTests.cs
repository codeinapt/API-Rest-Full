using Inventory.Application.Categories.Dtos;
using Inventory.Application.Categories.Ports;
using Inventory.Application.Categories.Queries.GetCategoryById;
using Inventory.Application.Common.Exceptions;
using Inventory.Domain.Common;
using NSubstitute;

namespace Inventory.Application.Tests.Categories.Queries.GetCategoryById;

public sealed class GetCategoryByIdHandlerTests
{
    private const int CategoryId = 11;
    private static readonly GetCategoryByIdQuery Query = new(CategoryId);

    private readonly ICategoryReadRepository categoryReadRepository = Substitute.For<ICategoryReadRepository>();
    private readonly GetCategoryByIdHandler handler;

    public GetCategoryByIdHandlerTests()
    {
        handler = new GetCategoryByIdHandler(categoryReadRepository);
    }

    [Fact]
    public async Task Handle_WhenCategoryExists_ReturnsIt()
    {
        var expectedCategory = new CategoryResponse(CategoryId, 7, "WH", "Tools", true);
        categoryReadRepository.GetByIdAsync(CategoryId, Arg.Any<CancellationToken>()).Returns(expectedCategory);
        var category = await handler.Handle(Query, CancellationToken.None);
        category.ShouldBe(expectedCategory);
    }

    [Fact]
    public async Task Handle_WhenCategoryIsInactive_StillReturnsIt()
    {
        var inactiveCategory = new CategoryResponse(CategoryId, 7, "WH", "Tools", false);
        categoryReadRepository.GetByIdAsync(CategoryId, Arg.Any<CancellationToken>()).Returns(inactiveCategory);
        var category = await handler.Handle(Query, CancellationToken.None);
        category.IsActive.ShouldBeFalse();
    }

    [Fact]
    public async Task Handle_WhenCategoryDoesNotExist_ThrowsCategoryNotFoundWithId()
    {
        categoryReadRepository.GetByIdAsync(CategoryId, Arg.Any<CancellationToken>()).Returns((CategoryResponse?)null);
        var exception = await Should.ThrowAsync<NotFoundException>(handler.Handle(Query, CancellationToken.None).AsTask());
        exception.Code.ShouldBe(ErrorCode.CategoryNotFound);
        exception.MessageArguments.ShouldBe([CategoryId]);
    }
}
