using Inventory.Application.Common.Interfaces;

namespace Inventory.Infrastructure.Common;

internal sealed class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}
