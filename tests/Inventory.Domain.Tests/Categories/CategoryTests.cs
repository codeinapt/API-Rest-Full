using Inventory.Domain.Areas;
using Inventory.Domain.Categories;
using Inventory.Domain.Common;
using Inventory.Domain.Tests.Common;

namespace Inventory.Domain.Tests.Categories;

public sealed class CategoryTests
{
    private const string ValidName = "Dairy";
    private static readonly DateTime CreationTime = new(2026, 10, 27, 9, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime UpdateTime = new(2026, 10, 28, 9, 0, 0, DateTimeKind.Utc);

    public static TheoryData<string> InvalidNames()
    {
        return new TheoryData<string>
        {
            string.Empty,
            "   ",
            "No",
            new string('N', Category.NameMaximumLength + 1),
        };
    }

    [Fact]
    public void Create_WithValidData_ReturnsActiveCategoryWithGivenValues()
    {
        var category = Category.Create(CreateActiveArea(), ValidName, CreationTime);
        category.AreaId.ShouldBe(1);
        category.Name.ShouldBe(ValidName);
        category.IsActive.ShouldBeTrue();
        category.CreatedAt.ShouldBe(CreationTime);
        category.UpdatedAt.ShouldBeNull();
    }

    [Theory]
    [InlineData("Abc")]
    [InlineData("Dairy products")]
    public void Create_WithNameAtOrAboveMinimumLength_Succeeds(string name)
    {
        var category = Category.Create(CreateActiveArea(), name, CreationTime);
        category.Name.ShouldBe(name);
    }

    [Fact]
    public void Create_WithNameAtMaximumLength_Succeeds()
    {
        var name = new string('N', Category.NameMaximumLength);
        var category = Category.Create(CreateActiveArea(), name, CreationTime);
        category.Name.ShouldBe(name);
    }

    [Theory]
    [MemberData(nameof(InvalidNames))]
    public void Create_WithInvalidName_ThrowsValidationError(string name)
    {
        var exception = Should.Throw<DomainException>(() => Category.Create(CreateActiveArea(), name, CreationTime));
        exception.Code.ShouldBe(ErrorCode.ValidationError);
    }

    [Fact]
    public void Create_WithInactiveArea_ThrowsReferencedAreaInvalidWithAreaId()
    {
        var area = CreateInactiveArea();
        var exception = Should.Throw<DomainException>(() => Category.Create(area, ValidName, CreationTime));
        exception.Code.ShouldBe(ErrorCode.ReferencedAreaInvalid);
        exception.MessageArguments.ShouldBe([1]);
    }

    [Fact]
    public void Update_WhenActive_ChangesNameAndUpdatedAt()
    {
        var area = CreateActiveArea();
        var category = Category.Create(area, ValidName, CreationTime);
        category.Update(area, "Grains", UpdateTime);
        category.Name.ShouldBe("Grains");
        category.AreaId.ShouldBe(1);
        category.UpdatedAt.ShouldBe(UpdateTime);
    }

    [Fact]
    public void Update_WithAnotherActiveArea_MovesCategoryToThatArea()
    {
        var category = Category.Create(CreateActiveArea(), ValidName, CreationTime);
        var targetArea = PersistedIdentity.Assign(Area.Create("TOOLS", "Tools", CreationTime), 2);
        category.Update(targetArea, ValidName, UpdateTime);
        category.AreaId.ShouldBe(2);
        category.UpdatedAt.ShouldBe(UpdateTime);
    }

    [Theory]
    [MemberData(nameof(InvalidNames))]
    public void Update_WithInvalidName_ThrowsValidationError(string name)
    {
        var area = CreateActiveArea();
        var category = Category.Create(area, ValidName, CreationTime);
        var exception = Should.Throw<DomainException>(() => category.Update(area, name, UpdateTime));
        exception.Code.ShouldBe(ErrorCode.ValidationError);
    }

    [Fact]
    public void Update_WithInvalidName_KeepsPreviousState()
    {
        var area = CreateActiveArea();
        var category = Category.Create(area, ValidName, CreationTime);
        Should.Throw<DomainException>(() => category.Update(area, "No", UpdateTime));
        category.Name.ShouldBe(ValidName);
        category.UpdatedAt.ShouldBeNull();
    }

    [Fact]
    public void Update_WithInactiveArea_ThrowsReferencedAreaInvalidWithAreaId()
    {
        var category = Category.Create(CreateActiveArea(), ValidName, CreationTime);
        var inactiveArea = CreateInactiveArea();
        var exception = Should.Throw<DomainException>(() => category.Update(inactiveArea, "Grains", UpdateTime));
        exception.Code.ShouldBe(ErrorCode.ReferencedAreaInvalid);
        exception.MessageArguments.ShouldBe([1]);
    }

    [Fact]
    public void Update_WithInactiveArea_KeepsPreviousState()
    {
        var category = Category.Create(CreateActiveArea(), ValidName, CreationTime);
        Should.Throw<DomainException>(() => category.Update(CreateInactiveArea(), "Grains", UpdateTime));
        category.Name.ShouldBe(ValidName);
        category.UpdatedAt.ShouldBeNull();
    }

    [Fact]
    public void Update_WhenInactive_ThrowsCategoryInactiveWithCategoryId()
    {
        var area = CreateActiveArea();
        var category = CreateInactiveCategory(area);
        var exception = Should.Throw<DomainException>(() => category.Update(area, "Grains", UpdateTime));
        exception.Code.ShouldBe(ErrorCode.CategoryInactive);
        exception.MessageArguments.ShouldBe([10]);
    }

    [Fact]
    public void Deactivate_WithoutActiveProducts_MarksCategoryInactive()
    {
        var category = Category.Create(CreateActiveArea(), ValidName, CreationTime);
        category.Deactivate(0);
        category.IsActive.ShouldBeFalse();
    }

    [Fact]
    public void Deactivate_WithActiveProducts_ThrowsCategoryHasActiveProducts()
    {
        var category = Category.Create(CreateActiveArea(), ValidName, CreationTime);
        var exception = Should.Throw<DomainException>(() => category.Deactivate(1));
        exception.Code.ShouldBe(ErrorCode.CategoryHasActiveProducts);
        exception.MessageArguments.ShouldBeEmpty();
    }

    [Fact]
    public void Deactivate_WithActiveProducts_KeepsCategoryActive()
    {
        var category = Category.Create(CreateActiveArea(), ValidName, CreationTime);
        Should.Throw<DomainException>(() => category.Deactivate(3));
        category.IsActive.ShouldBeTrue();
    }

    [Fact]
    public void Deactivate_WhenInactive_ThrowsCategoryInactiveWithCategoryId()
    {
        var category = CreateInactiveCategory(CreateActiveArea());
        var exception = Should.Throw<DomainException>(() => category.Deactivate(0));
        exception.Code.ShouldBe(ErrorCode.CategoryInactive);
        exception.MessageArguments.ShouldBe([10]);
    }

    private static Area CreateActiveArea()
    {
        return PersistedIdentity.Assign(Area.Create("FOOD", "Food", CreationTime), 1);
    }

    private static Area CreateInactiveArea()
    {
        var area = CreateActiveArea();
        area.Deactivate(0);

        return area;
    }

    private static Category CreateInactiveCategory(Area area)
    {
        var category = PersistedIdentity.Assign(Category.Create(area, ValidName, CreationTime), 10);
        category.Deactivate(0);

        return category;
    }
}
