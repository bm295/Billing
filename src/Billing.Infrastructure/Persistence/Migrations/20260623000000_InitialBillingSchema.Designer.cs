using Billing.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Billing.Infrastructure.Persistence.Migrations;

[DbContext(typeof(BillingDbContext))]
[Migration("20260623000000_InitialBillingSchema")]
public partial class InitialBillingSchema
{
    /// <inheritdoc />
    protected override void BuildTargetModel(ModelBuilder modelBuilder)
    {
        new BillingDbContextModelSnapshotAccessor().Build(modelBuilder);
    }

    private sealed class BillingDbContextModelSnapshotAccessor : BillingDbContextModelSnapshot
    {
        public void Build(ModelBuilder modelBuilder) => BuildModel(modelBuilder);
    }
}
