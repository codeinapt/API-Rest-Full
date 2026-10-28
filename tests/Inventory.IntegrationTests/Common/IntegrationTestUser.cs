using Inventory.Application.Common.Interfaces;

namespace Inventory.IntegrationTests.Common;

internal sealed class IntegrationTestUser : ICurrentUser
{
    public const string TestSubject = "integration-test";

    public string Subject => TestSubject;

    public IReadOnlyCollection<string> Roles { get; } = [];

    public IReadOnlyCollection<string> AreaCodes { get; } = [SqlServerFixture.TestAreaCode];
}
