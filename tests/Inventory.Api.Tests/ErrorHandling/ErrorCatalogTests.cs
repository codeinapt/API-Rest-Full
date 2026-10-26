using Inventory.Api.ErrorHandling;
using Inventory.Domain.Common;

namespace Inventory.Api.Tests.ErrorHandling;

public sealed class ErrorCatalogTests
{
    [Fact]
    public void Texts_ContainsAnEntryForEveryErrorCode()
    {
        ErrorCatalog.Texts.Keys.ShouldBe(Enum.GetValues<ErrorCode>(), ignoreOrder: true);
    }

    [Fact]
    public void Texts_HasNoEmptyTitleOrDetail()
    {
        ErrorCatalog.Texts.Values.ShouldAllBe(text => !string.IsNullOrWhiteSpace(text.Title) && !string.IsNullOrWhiteSpace(text.DetailTemplate));
    }
}
