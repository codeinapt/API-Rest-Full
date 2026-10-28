namespace Inventory.Domain.Movements;

public sealed record MovementEntry(int Quantity, string? Notes, DateTime CreatedAt, string CreatedBy);
