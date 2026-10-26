namespace Inventory.Domain.Common;

public class DomainException(ErrorCode code, params object[] messageArguments) : InventoryException(code, messageArguments);
