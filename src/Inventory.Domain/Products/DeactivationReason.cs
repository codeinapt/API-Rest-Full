namespace Inventory.Domain.Products;

public enum DeactivationReason : byte
{
    Expired = 1,
    Damaged = 2,
    Discontinued = 3,
    Recalled = 4,
    Other = 5,
}
