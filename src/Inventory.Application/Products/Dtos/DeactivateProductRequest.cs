using Inventory.Domain.Products;

namespace Inventory.Application.Products.Dtos;

public sealed record DeactivateProductRequest(DeactivationReason Reason, string? Notes);
