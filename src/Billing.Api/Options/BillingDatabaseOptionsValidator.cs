using Microsoft.Extensions.Options;

namespace Billing.Api.Options;

public sealed class BillingDatabaseOptionsValidator : IValidateOptions<BillingDatabaseOptions>
{
    private readonly IHostEnvironment _environment;

    public BillingDatabaseOptionsValidator(IHostEnvironment environment)
    {
        _environment = environment;
    }

    public ValidateOptionsResult Validate(string? name, BillingDatabaseOptions options)
    {
        if (!_environment.IsProduction())
        {
            return ValidateOptionsResult.Success;
        }

        if (string.IsNullOrWhiteSpace(options.ConnectionString))
        {
            return ValidateOptionsResult.Fail(
                $"Connection string '{BillingDatabaseOptions.ConnectionStringName}' must be configured in Production.");
        }

        return ValidateOptionsResult.Success;
    }
}
