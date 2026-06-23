namespace Billing.Api.Options;

public sealed class BillingDatabaseOptions
{
    public const string ConnectionStringName = "BillingDb";

    public string? ConnectionString { get; set; }
}
