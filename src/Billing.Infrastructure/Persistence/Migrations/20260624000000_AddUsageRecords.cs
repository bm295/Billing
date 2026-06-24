using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Billing.Infrastructure.Persistence.Migrations;

public partial class AddUsageRecords : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "UsageRecords",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                SubscriptionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                MetricName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                Quantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                Timestamp = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                IdempotencyKey = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_UsageRecords", x => x.Id);
                table.ForeignKey(
                    name: "FK_UsageRecords_Customers_CustomerId",
                    column: x => x.CustomerId,
                    principalTable: "Customers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_UsageRecords_Subscriptions_SubscriptionId",
                    column: x => x.SubscriptionId,
                    principalTable: "Subscriptions",
                    principalColumn: "Id");
            });

        migrationBuilder.CreateIndex(
            name: "IX_UsageRecords_CustomerId",
            table: "UsageRecords",
            column: "CustomerId");

        migrationBuilder.CreateIndex(
            name: "IX_UsageRecords_IdempotencyKey",
            table: "UsageRecords",
            column: "IdempotencyKey",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_UsageRecords_SubscriptionId",
            table: "UsageRecords",
            column: "SubscriptionId");

        migrationBuilder.CreateIndex(
            name: "IX_UsageRecords_Timestamp",
            table: "UsageRecords",
            column: "Timestamp");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "UsageRecords");
    }
}
