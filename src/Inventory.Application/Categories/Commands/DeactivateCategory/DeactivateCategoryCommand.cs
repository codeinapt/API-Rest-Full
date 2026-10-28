using Mediator;

namespace Inventory.Application.Categories.Commands.DeactivateCategory;

public sealed record DeactivateCategoryCommand(int CategoryId) : IRequest;
