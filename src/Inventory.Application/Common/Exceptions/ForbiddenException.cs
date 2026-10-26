using Inventory.Domain.Common;

namespace Inventory.Application.Common.Exceptions;

public sealed class ForbiddenException(ErrorCode code, params object[] messageArguments) : InventoryException(code, messageArguments);
