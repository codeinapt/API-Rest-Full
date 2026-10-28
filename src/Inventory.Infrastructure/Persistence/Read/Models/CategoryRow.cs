namespace Inventory.Infrastructure.Persistence.Read.Models;

internal sealed class CategoryRow
{
    public int Id { get; init; }

    public int AreaId { get; init; }

    public required string Name { get; init; }

    public bool IsActive { get; init; }

    public DateTime CreatedAt { get; init; }

    public DateTime? UpdatedAt { get; init; }
}
