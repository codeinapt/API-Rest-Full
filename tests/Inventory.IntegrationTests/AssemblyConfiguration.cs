using Inventory.IntegrationTests.Common;
using Xunit.Sdk;
using Xunit.v3;

[assembly: AssemblyFixture(typeof(SqlServerFixture))]
[assembly: Parallelization(Mode = ParallelMode.None)]
