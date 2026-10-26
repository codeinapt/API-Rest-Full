using Inventory.Domain.Common;

namespace Inventory.Application.Common.Exceptions;

public sealed class InvalidReferenceException(ErrorCode code, object id) : InventoryException(code, [id]);
