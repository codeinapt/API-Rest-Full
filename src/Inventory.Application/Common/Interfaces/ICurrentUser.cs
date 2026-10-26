namespace Inventory.Application.Common.Interfaces;

public interface ICurrentUser
{
    string Subject { get; }

    IReadOnlyCollection<string> Roles { get; }

    IReadOnlyCollection<string> AreaCodes { get; }
}
