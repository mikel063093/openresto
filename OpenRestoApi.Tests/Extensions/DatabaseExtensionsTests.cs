using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using OpenRestoApi.Extensions;
using OpenRestoApi.Infrastructure.Persistence;

namespace OpenRestoApi.Tests.Extensions;

public class DatabaseExtensionsTests
{
    [Fact]
    public void GetAppConnectionString_UsesConfigValue_WhenPresent()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:DefaultConnection"] = "ConnStr" })
            .Build();
        var env = new Mock<IWebHostEnvironment>().Object;

        var result = config.GetAppConnectionString(env);

        Assert.Equal("ConnStr", result);
    }

    [Fact]
    public void GetAppConnectionString_UsesEnvVar_WhenConfigMissing()
    {
        var config = new ConfigurationBuilder().Build();
        var env = new Mock<IWebHostEnvironment>().Object;
        Environment.SetEnvironmentVariable("CONNECTION_STRING", "EnvStr");

        try
        {
            var result = config.GetAppConnectionString(env);
            Assert.Equal("EnvStr", result);
        }
        finally
        {
            Environment.SetEnvironmentVariable("CONNECTION_STRING", null);
        }
    }

    [Theory]
    [InlineData(null, DatabaseProvider.Sqlite)]
    [InlineData("sqlite", DatabaseProvider.Sqlite)]
    [InlineData("POSTGRES", DatabaseProvider.Postgres)]
    public void GetDatabaseProvider_ParsesConfiguredProvider_AndDefaultsToSqlite(string? configuredProvider, DatabaseProvider expected)
    {
        var values = configuredProvider is null
            ? new Dictionary<string, string?>()
            : new Dictionary<string, string?> { ["DATABASE_PROVIDER"] = configuredProvider };
        var config = new ConfigurationBuilder().AddInMemoryCollection(values).Build();

        Assert.Equal(expected, config.GetDatabaseProvider());
    }

    [Fact]
    public void GetAppConnectionString_RequiresConfiguredValue_ForPostgres()
    {
        var config = new ConfigurationBuilder().Build();
        var environment = new Mock<IWebHostEnvironment>().Object;

        var exception = Assert.Throws<InvalidOperationException>(() =>
            config.GetAppConnectionString(DatabaseProvider.Postgres, environment));

        Assert.Contains("DATABASE_PROVIDER=postgres", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void GetDatabaseProvider_ThrowsForUnsupportedProvider()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["DATABASE_PROVIDER"] = "sqlserver" })
            .Build();

        var exception = Assert.Throws<InvalidOperationException>(() => config.GetDatabaseProvider());

        Assert.Contains("DATABASE_PROVIDER", exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(DatabaseProvider.Sqlite, "Microsoft.EntityFrameworkCore.Sqlite")]
    [InlineData(DatabaseProvider.Postgres, "Npgsql.EntityFrameworkCore.PostgreSQL")]
    public void AddDatabaseSetup_ConfiguresRequestedProvider(DatabaseProvider provider, string expectedProviderName)
    {
        var services = new ServiceCollection();
        var environment = new Mock<IWebHostEnvironment>();
        environment.SetupGet(value => value.EnvironmentName).Returns("Testing");

        services.AddDatabaseSetup("Host=localhost;Database=openresto;Username=test;Password=test", provider, environment.Object);

        using ServiceProvider serviceProvider = services.BuildServiceProvider();
        using IServiceScope scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        Assert.Equal(expectedProviderName, db.Database.ProviderName);
    }

    [Fact]
    public void GetAppConnectionString_UsesDefault_WhenAllMissing()
    {
        var config = new ConfigurationBuilder().Build();
        var envMock = new Mock<IWebHostEnvironment>();
        envMock.Setup(e => e.EnvironmentName).Returns("Development");

        var result = config.GetAppConnectionString(envMock.Object);
        Assert.Equal("Data Source=./openresto.db", result);
        
        envMock.Setup(e => e.EnvironmentName).Returns("Production");
        result = config.GetAppConnectionString(envMock.Object);
        Assert.Equal("Data Source=/data/openresto.db", result);
    }
}
