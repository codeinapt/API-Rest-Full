namespace Inventory.Domain.Common;

public abstract class InventoryException(ErrorCode code, IReadOnlyList<object> messageArguments) : Exception(code.ToString())
{
    public ErrorCode Code { get; } = code;

    public IReadOnlyList<object> MessageArguments { get; } = messageArguments;
}
