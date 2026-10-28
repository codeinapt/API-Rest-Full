namespace Inventory.Domain.Movements;

public enum AdjustmentReason : byte
{
    PhysicalCountCorrection = 1,
    Damage = 2,
    Expiration = 3,
    LossOrTheft = 4,
    Found = 5,
    DataEntryError = 6,
}
