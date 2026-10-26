using Inventory.Domain.Common;

namespace Inventory.Application.Common.Exceptions;

public sealed class NotFoundException(ErrorCode code, object id) : InventoryException(code, [id]);
