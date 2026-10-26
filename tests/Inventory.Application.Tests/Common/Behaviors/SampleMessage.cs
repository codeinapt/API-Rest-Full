using Mediator;

namespace Inventory.Application.Tests.Common.Behaviors;

public sealed record SampleMessage(string Name, string Sku) : IRequest<string>;
