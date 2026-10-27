namespace Inventory.Domain.Products;

public sealed record ProductDeactivation(DeactivationReason Reason, string? Notes, DateTime At, string By);
