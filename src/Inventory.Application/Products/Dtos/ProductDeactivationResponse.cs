using Inventory.Domain.Products;

namespace Inventory.Application.Products.Dtos;

public sealed record ProductDeactivationResponse(DeactivationReason Reason, string? Notes, DateTime At, string By);
