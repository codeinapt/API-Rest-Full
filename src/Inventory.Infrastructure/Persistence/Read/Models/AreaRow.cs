namespace Inventory.Infrastructure.Persistence.Read.Models;

internal sealed class AreaRow
{
    public int Id { get; init; }

    public required string Code { get; init; }

    public required string Name { get; init; }

    public bool IsActive { get; init; }

    public DateTime CreatedAt { get; init; }

    public DateTime? UpdatedAt { get; init; }
}
