using Billing.Api.Options;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace Billing.Api.Tests;

public sealed class BillingDatabaseOptionsValidatorTests
{
    [Fact]
    public void Validate_ReturnsFailure_WhenProductionConnectionStringIsMissing()
    {
        var validator = new BillingDatabaseOptionsValidator(new TestHostEnvironment(Environments.Production));

        var result = validator.Validate(null, new BillingDatabaseOptions());

        Assert.True(result.Failed);
        Assert.Contains("Connection string 'BillingDb' must be configured in Production.", result.Failures);
    }

    [Fact]
    public void Validate_ReturnsSuccess_WhenProductionConnectionStringIsConfigured()
    {
        var validator = new BillingDatabaseOptionsValidator(new TestHostEnvironment(Environments.Production));

        var result = validator.Validate(null, new BillingDatabaseOptions
        {
            ConnectionString = "Server=sql;Database=BillingDb;User Id=billing;Password=secret;"
        });

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Validate_ReturnsSuccess_WhenNonProductionConnectionStringIsMissing()
    {
        var validator = new BillingDatabaseOptionsValidator(new TestHostEnvironment(Environments.Development));

        var result = validator.Validate(null, new BillingDatabaseOptions());

        Assert.True(result.Succeeded);
    }

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public TestHostEnvironment(string environmentName)
        {
            EnvironmentName = environmentName;
        }

        public string ApplicationName { get; set; } = "Billing.Api.Tests";

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();

        public string ContentRootPath { get; set; } = string.Empty;

        public string EnvironmentName { get; set; }
    }
}
