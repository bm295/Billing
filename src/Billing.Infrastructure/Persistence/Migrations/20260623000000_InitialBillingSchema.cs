using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Billing.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class InitialBillingSchema : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Customers",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Email = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: false),
                CompanyName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                BillingAddress = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                PaymentMethodId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Customers", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "IdempotencyRecords",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Key = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                RequestHash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                StatusCode = table.Column<int>(type: "int", nullable: true),
                ResponsePaymentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                ErrorCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                ErrorMessage = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_IdempotencyRecords", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Products",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                Active = table.Column<bool>(type: "bit", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Products", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "PricePlans",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                BillingType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                BillingInterval = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                UsageUnit = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PricePlans", x => x.Id);
                table.ForeignKey(
                    name: "FK_PricePlans_Products_ProductId",
                    column: x => x.ProductId,
                    principalTable: "Products",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "Subscriptions",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                PricePlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                CurrentPeriodStart = table.Column<DateOnly>(type: "date", nullable: false),
                CurrentPeriodEnd = table.Column<DateOnly>(type: "date", nullable: false),
                CancelAtPeriodEnd = table.Column<bool>(type: "bit", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Subscriptions", x => x.Id);
                table.ForeignKey(
                    name: "FK_Subscriptions_Customers_CustomerId",
                    column: x => x.CustomerId,
                    principalTable: "Customers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_Subscriptions_PricePlans_PricePlanId",
                    column: x => x.PricePlanId,
                    principalTable: "PricePlans",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "Invoices",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                SubscriptionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                AmountDue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                AmountPaid = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                BillingPeriodStart = table.Column<DateOnly>(type: "date", nullable: false),
                BillingPeriodEnd = table.Column<DateOnly>(type: "date", nullable: false),
                DueDate = table.Column<DateOnly>(type: "date", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Invoices", x => x.Id);
                table.ForeignKey(
                    name: "FK_Invoices_Customers_CustomerId",
                    column: x => x.CustomerId,
                    principalTable: "Customers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_Invoices_Subscriptions_SubscriptionId",
                    column: x => x.SubscriptionId,
                    principalTable: "Subscriptions",
                    principalColumn: "Id");
            });

        migrationBuilder.CreateTable(
            name: "InvoiceLines",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                InvoiceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                Quantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                UnitPrice = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_InvoiceLines", x => x.Id);
                table.ForeignKey(
                    name: "FK_InvoiceLines_Invoices_InvoiceId",
                    column: x => x.InvoiceId,
                    principalTable: "Invoices",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "Payments",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                InvoiceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                Provider = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                IdempotencyKey = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Payments", x => x.Id);
                table.ForeignKey(
                    name: "FK_Payments_Invoices_InvoiceId",
                    column: x => x.InvoiceId,
                    principalTable: "Invoices",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "PaymentAttempts",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                PaymentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                AttemptNumber = table.Column<int>(type: "int", nullable: false),
                Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                FailureReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PaymentAttempts", x => x.Id);
                table.ForeignKey(
                    name: "FK_PaymentAttempts_Payments_PaymentId",
                    column: x => x.PaymentId,
                    principalTable: "Payments",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_IdempotencyRecords_Key",
            table: "IdempotencyRecords",
            column: "Key",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_InvoiceLines_InvoiceId",
            table: "InvoiceLines",
            column: "InvoiceId");

        migrationBuilder.CreateIndex(
            name: "IX_Invoices_CustomerId",
            table: "Invoices",
            column: "CustomerId");

        migrationBuilder.CreateIndex(
            name: "IX_Invoices_Status",
            table: "Invoices",
            column: "Status");

        migrationBuilder.CreateIndex(
            name: "IX_Invoices_SubscriptionId_BillingPeriodStart_BillingPeriodEnd",
            table: "Invoices",
            columns: new[] { "SubscriptionId", "BillingPeriodStart", "BillingPeriodEnd" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_PaymentAttempts_PaymentId_AttemptNumber",
            table: "PaymentAttempts",
            columns: new[] { "PaymentId", "AttemptNumber" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Payments_IdempotencyKey",
            table: "Payments",
            column: "IdempotencyKey",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Payments_InvoiceId",
            table: "Payments",
            column: "InvoiceId");

        migrationBuilder.CreateIndex(
            name: "IX_PricePlans_ProductId",
            table: "PricePlans",
            column: "ProductId");

        migrationBuilder.CreateIndex(
            name: "IX_Subscriptions_CustomerId",
            table: "Subscriptions",
            column: "CustomerId");

        migrationBuilder.CreateIndex(
            name: "IX_Subscriptions_PricePlanId",
            table: "Subscriptions",
            column: "PricePlanId");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "InvoiceLines");
        migrationBuilder.DropTable(name: "IdempotencyRecords");
        migrationBuilder.DropTable(name: "PaymentAttempts");
        migrationBuilder.DropTable(name: "Payments");
        migrationBuilder.DropTable(name: "Invoices");
        migrationBuilder.DropTable(name: "Subscriptions");
        migrationBuilder.DropTable(name: "Customers");
        migrationBuilder.DropTable(name: "PricePlans");
        migrationBuilder.DropTable(name: "Products");
    }
}
