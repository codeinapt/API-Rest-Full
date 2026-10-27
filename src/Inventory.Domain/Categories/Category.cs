using Inventory.Domain.Areas;
using Inventory.Domain.Common;

namespace Inventory.Domain.Categories;

public sealed class Category
{
    public const int NameMinimumLength = 3;

    public const int NameMaximumLength = 100;

    private Category()
    {
    }

    public int Id { get; private set; }

    public int AreaId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public bool IsActive { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime? UpdatedAt { get; private set; }

    public static Category Create(Area area, string name, DateTime createdAt)
    {
        EnsureValidName(name);
        EnsureActiveArea(area);

        return new Category { AreaId = area.Id, Name = name, IsActive = true, CreatedAt = createdAt };
    }

    public void Update(Area area, string name, DateTime updatedAt)
    {
        EnsureActive();
        EnsureValidName(name);
        EnsureActiveArea(area);
        AreaId = area.Id;
        Name = name;
        UpdatedAt = updatedAt;
    }

    public void Deactivate(int activeProductCount)
    {
        EnsureActive();
        EnsureHasNoActiveProducts(activeProductCount);
        IsActive = false;
    }

    private static void EnsureValidName(string name)
    {
        var hasValidLength = name.Length is >= NameMinimumLength and <= NameMaximumLength;
        if (string.IsNullOrWhiteSpace(name) || !hasValidLength)
        {
            throw new DomainException(ErrorCode.ValidationError);
        }
    }

    private static void EnsureActiveArea(Area area)
    {
        if (!area.IsActive)
        {
            throw new DomainException(ErrorCode.ReferencedAreaInvalid, area.Id);
        }
    }

    private static void EnsureHasNoActiveProducts(int activeProductCount)
    {
        if (activeProductCount > 0)
        {
            throw new DomainException(ErrorCode.CategoryHasActiveProducts);
        }
    }

    private void EnsureActive()
    {
        if (!IsActive)
        {
            throw new DomainException(ErrorCode.CategoryInactive, Id);
        }
    }
}
