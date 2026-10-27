using System.Text.RegularExpressions;
using Inventory.Domain.Common;

namespace Inventory.Domain.Areas;

public sealed partial class Area
{
    public const int CodeMinimumLength = 2;

    public const int CodeMaximumLength = 10;

    public const string CodePattern = @"^[A-Z]+\z";

    public const int NameMinimumLength = 3;

    public const int NameMaximumLength = 100;

    private Area()
    {
    }

    public int Id { get; private set; }

    public string Code { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public bool IsActive { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime? UpdatedAt { get; private set; }

    public static Area Create(string code, string name, DateTime createdAt)
    {
        EnsureValidCode(code);
        EnsureValidName(name);

        return new Area { Code = code, Name = name, IsActive = true, CreatedAt = createdAt };
    }

    public void Rename(string name, DateTime updatedAt)
    {
        EnsureActive();
        EnsureValidName(name);
        Name = name;
        UpdatedAt = updatedAt;
    }

    public void Deactivate(int activeCategoryCount)
    {
        EnsureActive();
        EnsureHasNoActiveCategories(activeCategoryCount);
        IsActive = false;
    }

    private static void EnsureValidCode(string code)
    {
        var hasValidLength = code.Length is >= CodeMinimumLength and <= CodeMaximumLength;
        if (!hasValidLength || !CodeRegex().IsMatch(code))
        {
            throw new DomainException(ErrorCode.ValidationError);
        }
    }

    private static void EnsureValidName(string name)
    {
        var hasValidLength = name.Length is >= NameMinimumLength and <= NameMaximumLength;
        if (string.IsNullOrWhiteSpace(name) || !hasValidLength)
        {
            throw new DomainException(ErrorCode.ValidationError);
        }
    }

    private static void EnsureHasNoActiveCategories(int activeCategoryCount)
    {
        if (activeCategoryCount > 0)
        {
            throw new DomainException(ErrorCode.AreaHasActiveCategories);
        }
    }

    [GeneratedRegex(CodePattern)]
    private static partial Regex CodeRegex();

    private void EnsureActive()
    {
        if (!IsActive)
        {
            throw new DomainException(ErrorCode.AreaInactive, Id);
        }
    }
}
