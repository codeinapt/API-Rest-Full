using Inventory.Domain.Areas;
using Inventory.Domain.Common;

namespace Inventory.Domain.Tests.Areas;

public sealed class AreaTests
{
    private const string ValidCode = "WH";
    private const string ValidName = "Warehouse";
    private static readonly DateTime CreationTime = new(2026, 10, 27, 9, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime UpdateTime = new(2026, 10, 28, 9, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Create_WithValidData_ReturnsActiveAreaWithGivenValues()
    {
        var area = Area.Create(ValidCode, ValidName, CreationTime);
        area.Code.ShouldBe(ValidCode);
        area.Name.ShouldBe(ValidName);
        area.IsActive.ShouldBeTrue();
        area.CreatedAt.ShouldBe(CreationTime);
        area.UpdatedAt.ShouldBeNull();
    }

    [Theory]
    [InlineData("AB")]
    [InlineData("ABCDEFGHIJ")]
    public void Create_WithCodeAtLengthLimits_Succeeds(string code)
    {
        var area = Area.Create(code, ValidName, CreationTime);
        area.Code.ShouldBe(code);
    }

    [Theory]
    [InlineData("")]
    [InlineData("A")]
    [InlineData("ABCDEFGHIJK")]
    [InlineData("wh")]
    [InlineData("W1")]
    [InlineData("W-H")]
    [InlineData("ÄB")]
    public void Create_WithInvalidCode_ThrowsValidationError(string code)
    {
        var exception = Should.Throw<DomainException>(() => Area.Create(code, ValidName, CreationTime));
        exception.Code.ShouldBe(ErrorCode.ValidationError);
    }

    [Theory]
    [InlineData("Abc")]
    [InlineData("Main storage")]
    public void Create_WithNameAtOrAboveMinimumLength_Succeeds(string name)
    {
        var area = Area.Create(ValidCode, name, CreationTime);
        area.Name.ShouldBe(name);
    }

    [Fact]
    public void Create_WithNameAtMaximumLength_Succeeds()
    {
        var name = new string('N', Area.NameMaximumLength);
        var area = Area.Create(ValidCode, name, CreationTime);
        area.Name.ShouldBe(name);
    }

    [Theory]
    [MemberData(nameof(InvalidNames))]
    public void Create_WithInvalidName_ThrowsValidationError(string name)
    {
        var exception = Should.Throw<DomainException>(() => Area.Create(ValidCode, name, CreationTime));
        exception.Code.ShouldBe(ErrorCode.ValidationError);
    }

    [Fact]
    public void Rename_WhenActive_ChangesNameAndUpdatedAt()
    {
        var area = Area.Create(ValidCode, ValidName, CreationTime);
        area.Rename("Store", UpdateTime);
        area.Name.ShouldBe("Store");
        area.UpdatedAt.ShouldBe(UpdateTime);
    }

    [Fact]
    public void Rename_WhenActive_KeepsCodeUnchanged()
    {
        var area = Area.Create(ValidCode, ValidName, CreationTime);
        area.Rename("Store", UpdateTime);
        area.Code.ShouldBe(ValidCode);
    }

    [Theory]
    [MemberData(nameof(InvalidNames))]
    public void Rename_WithInvalidName_ThrowsValidationError(string name)
    {
        var area = Area.Create(ValidCode, ValidName, CreationTime);
        var exception = Should.Throw<DomainException>(() => area.Rename(name, UpdateTime));
        exception.Code.ShouldBe(ErrorCode.ValidationError);
    }

    [Fact]
    public void Rename_WithInvalidName_KeepsPreviousState()
    {
        var area = Area.Create(ValidCode, ValidName, CreationTime);
        Should.Throw<DomainException>(() => area.Rename("No", UpdateTime));
        area.Name.ShouldBe(ValidName);
        area.UpdatedAt.ShouldBeNull();
    }

    [Fact]
    public void Rename_WhenInactive_ThrowsAreaInactiveWithAreaId()
    {
        var area = CreateInactiveArea();
        var exception = Should.Throw<DomainException>(() => area.Rename("Store", UpdateTime));
        exception.Code.ShouldBe(ErrorCode.AreaInactive);
        exception.MessageArguments.ShouldBe([area.Id]);
    }

    [Fact]
    public void Deactivate_WithoutActiveCategories_MarksAreaInactive()
    {
        var area = Area.Create(ValidCode, ValidName, CreationTime);
        area.Deactivate(0);
        area.IsActive.ShouldBeFalse();
    }

    [Fact]
    public void Deactivate_WithActiveCategories_ThrowsAreaHasActiveCategories()
    {
        var area = Area.Create(ValidCode, ValidName, CreationTime);
        var exception = Should.Throw<DomainException>(() => area.Deactivate(1));
        exception.Code.ShouldBe(ErrorCode.AreaHasActiveCategories);
        exception.MessageArguments.ShouldBeEmpty();
    }

    [Fact]
    public void Deactivate_WithActiveCategories_KeepsAreaActive()
    {
        var area = Area.Create(ValidCode, ValidName, CreationTime);
        Should.Throw<DomainException>(() => area.Deactivate(3));
        area.IsActive.ShouldBeTrue();
    }

    [Fact]
    public void Deactivate_WhenInactive_ThrowsAreaInactiveWithAreaId()
    {
        var area = CreateInactiveArea();
        var exception = Should.Throw<DomainException>(() => area.Deactivate(0));
        exception.Code.ShouldBe(ErrorCode.AreaInactive);
        exception.MessageArguments.ShouldBe([area.Id]);
    }

    public static TheoryData<string> InvalidNames()
    {
        return new TheoryData<string>
        {
            string.Empty,
            "   ",
            "No",
            new string('N', Area.NameMaximumLength + 1),
        };
    }

    private static Area CreateInactiveArea()
    {
        var area = Area.Create(ValidCode, ValidName, CreationTime);
        area.Deactivate(0);

        return area;
    }
}
