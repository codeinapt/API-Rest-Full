using Inventory.Domain.Common;

namespace Inventory.Domain.Tests.Common;

public sealed class DomainExceptionTests
{
    [Fact]
    public void Constructor_WithCodeAndArguments_ExposesBoth()
    {
        var exception = new DomainException(ErrorCode.InsufficientStock, 3, 5);
        exception.Code.ShouldBe(ErrorCode.InsufficientStock);
        exception.MessageArguments.ShouldBe([3, 5]);
    }

    [Fact]
    public void Constructor_WithoutArguments_ExposesAnEmptyArgumentList()
    {
        var exception = new DomainException(ErrorCode.AreaHasActiveCategories);
        exception.MessageArguments.ShouldBeEmpty();
    }

    [Fact]
    public void Message_IsTheErrorCodeNameInEnglish()
    {
        var exception = new DomainException(ErrorCode.ProductInactive, 5);
        exception.Message.ShouldBe(nameof(ErrorCode.ProductInactive));
    }
}
