namespace OpenRestoApi.Tests.Postgres;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class PostgresIntegrationCollection : ICollectionFixture<PostgresTestHarness>
{
    public const string Name = "PostgresIntegration";
}
