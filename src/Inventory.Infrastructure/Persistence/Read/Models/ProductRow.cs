namespace Inventory.Infrastructure.Persistence.Read.Models;

internal sealed class ProductRow
{
    public int Id { get; init; }

    public required string Sku { get; init; }

    public required string Name { get; init; }

    public string? Description { get; init; }

    public int CategoryId { get; init; }

    public int Stock { get; init; }

    public int MinimumStock { get; init; }

    public bool IsActive { get; init; }

    public byte? DeactivationReason { get; init; }

    public string? DeactivationNotes { get; init; }

    public DateTime? DeactivatedAt { get; init; }

    public string? DeactivatedBy { get; init; }

    public DateTime? ReactivatedAt { get; init; }

    public string? ReactivatedBy { get; init; }

    public DateTime CreatedAt { get; init; }

    public DateTime? UpdatedAt { get; init; }
}
