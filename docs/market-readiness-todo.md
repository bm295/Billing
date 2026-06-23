# Market Readiness To Do List

This checklist breaks the remaining work into small, clear, implementation-ready tasks to make the SaaS Billing Engine ready for a market launch. Each task is intentionally specific enough to assign to an engineer and verify in a pull request.

## 1. Production Data Storage

- [x] Replace the in-memory database registration in `src/Billing.Api/Host/Program.cs` with a SQL Server registration that reads `ConnectionStrings:BillingDb` from configuration.
- [x] Add a `ConnectionStrings` section with a `BillingDb` placeholder to `src/Billing.Api/appsettings.json`.
- [x] Create an EF Core migration named `InitialBillingSchema` for the existing `BillingDbContext` model.
- [x] Add startup validation class `BillingDatabaseOptionsValidator` to fail fast when the production connection string is missing.
- [x] Create a deployment runbook section that explains how to run database migrations before deploying the API.

## 2. Customer Management

- [x] Create request class `CreateCustomerRequest` with fields `Email`, `CompanyName`, `BillingAddress`, and `PaymentMethodId`.
- [x] Create response class `CustomerResponse` with fields `Id`, `Email`, `CompanyName`, `BillingAddress`, `PaymentMethodId`, `Status`, and `CreatedAt`.
- [x] Create interface `ICustomerService` with methods `CreateCustomerAsync`, `GetCustomerAsync`, and `ListCustomersAsync`.
- [x] Create class `CustomerService` that validates unique customer email addresses before inserting a customer.
- [x] Create endpoint class `CustomerEndpoints` with routes `POST /customers`, `GET /customers/{customerId}`, and `GET /customers`.
- [x] Add tests in `CustomerEndpointTests` for creating, fetching, listing, and rejecting duplicate customer emails.

## 3. Product and Price Plan Management

- [x] Create request class `CreateProductRequest` with fields `Name`, `Description`, and `Active`.
- [x] Create request class `CreatePricePlanRequest` with fields `ProductId`, `BillingType`, `Amount`, `Currency`, `BillingInterval`, and `UsageUnit`.
- [x] Create response classes `ProductResponse` and `PricePlanResponse` for API output.
- [x] Create interface `ICatalogService` for product and plan operations.
- [x] Create class `CatalogService` to create, update, deactivate, and list products and price plans.
- [x] Create endpoint class `CatalogEndpoints` with routes for products and price plans under `/catalog`.
- [ ] Add validation that `Currency` is a three-letter ISO code and `Amount` is not negative.
- [ ] Add tests in `CatalogEndpointTests` for active products, inactive products, recurring plans, and usage-based plans.

## 4. Usage-Based Billing

- [ ] Create domain entity `UsageRecord` with fields `Id`, `CustomerId`, `SubscriptionId`, `MetricName`, `Quantity`, `Timestamp`, and `IdempotencyKey`.
- [ ] Add `DbSet<UsageRecord>` and model configuration for indexes on `SubscriptionId`, `Timestamp`, and `IdempotencyKey`.
- [ ] Create request class `ReportUsageRequest` with fields `SubscriptionId`, `MetricName`, `Quantity`, and `Timestamp`.
- [ ] Create response class `UsageRecordResponse` for accepted usage records.
- [ ] Create interface `IUsageService` with methods `ReportUsageAsync` and `GetUsageForBillingPeriodAsync`.
- [ ] Create class `UsageService` that stores usage records idempotently by `Idempotency-Key`.
- [ ] Create endpoint class `UsageEndpoints` with route `POST /usage`.
- [ ] Update `InvoiceService` to include usage invoice lines for usage-based price plans.
- [ ] Add tests in `UsageEndpointTests` for usage ingestion, duplicate idempotency keys, invalid quantities, and invoice usage totals.

## 5. Subscription Lifecycle

- [ ] Extend `SubscriptionEndpoints` with `GET /subscriptions/{subscriptionId}` and `GET /customers/{customerId}/subscriptions`.
- [ ] Create request class `CancelSubscriptionRequest` with field `CancelAtPeriodEnd`.
- [ ] Add method `CancelSubscriptionAsync` to the subscription service.
- [ ] Add method `ChangeSubscriptionPlanAsync` to support plan upgrades and downgrades.
- [ ] Create class `ProrationCalculator` to calculate unused credit and remaining plan cost when changing plans mid-cycle.
- [ ] Add tests in `SubscriptionEndpointTests` for cancellation, plan changes, and proration results.

## 6. Invoice Lifecycle

- [ ] Add endpoint `GET /invoices/{invoiceId}` to fetch one invoice with lines.
- [ ] Add endpoint `GET /customers/{customerId}/invoices` to list customer invoices.
- [ ] Create request class `FinalizeInvoiceRequest` for explicitly finalizing draft invoices.
- [ ] Add invoice status `Draft` and only allow payment after an invoice is finalized as `Open`.
- [ ] Create class `InvoiceNumberGenerator` to generate human-readable invoice numbers such as `INV-2026-000001`.
- [ ] Add invoice number uniqueness constraints to `BillingDbContext`.
- [ ] Add tests in `InvoiceEndpointTests` for invoice retrieval, customer invoice listing, draft invoices, finalized invoices, and invoice number uniqueness.

## 7. Payment Gateway Integration

- [ ] Create interface `IPaymentGateway` with method `ChargeAsync` that accepts amount, currency, payment method token, and metadata.
- [ ] Create class `TestPaymentGateway` for local development and automated tests.
- [ ] Create class `StripePaymentGateway` or `ExternalPaymentGateway` behind configuration for production use.
- [ ] Refactor `PaymentService` to call `IPaymentGateway` instead of directly simulating success or failure.
- [ ] Store external gateway charge IDs on `Payment` and `PaymentAttempt` records.
- [ ] Add payment gateway timeout handling and map gateway failures to stable API error codes.
- [ ] Add tests in `PaymentEndpointTests` for successful gateway charges, gateway declines, gateway timeouts, and idempotent retries.

## 8. Payment Retry and Dunning

- [ ] Create class `PaymentRetryPolicy` with retry offsets of day 1, day 3, and day 7 after failure.
- [ ] Add fields `NextRetryAt` and `FinalAttemptAt` to the `Payment` entity.
- [ ] Create interface `IPaymentRetryService` with method `ProcessDueRetriesAsync`.
- [ ] Create class `PaymentRetryService` to retry failed payments whose `NextRetryAt` is due.
- [ ] Create class `SubscriptionDunningService` to mark subscriptions as `PastDue` after retry exhaustion.
- [ ] Add background job registration for payment retries using the selected job scheduler.
- [ ] Add tests for retry scheduling, successful retry recovery, exhausted retries, and subscription `PastDue` transitions.

## 9. Background Billing Jobs

- [ ] Choose a scheduler package such as Quartz or Hangfire and add it to the infrastructure project.
- [ ] Create class `RecurringBillingJob` that finds subscriptions whose billing period has ended and generates invoices.
- [ ] Create class `UsageAggregationJob` that prepares usage totals for invoice generation.
- [ ] Create class `DunningReminderJob` that sends reminders for overdue invoices.
- [ ] Add job configuration options for schedule, batch size, and retry limits.
- [ ] Add integration tests that run each job against a seeded test database.

## 10. Security and Compliance

- [ ] Add authentication middleware to require authenticated API access outside health endpoints.
- [ ] Add authorization policies for customer-scoped reads and billing-admin writes.
- [ ] Ensure raw card data is never accepted by API contracts; accept tokenized `PaymentMethodId` only.
- [ ] Add request validation that rejects suspiciously long strings and malformed IDs.
- [ ] Add rate limiting to payment and usage ingestion endpoints.
- [ ] Add audit fields `CreatedBy`, `UpdatedBy`, and `UpdatedAt` where market operations require traceability.
- [ ] Add a security document covering PCI scope, tokenized payments, secret storage, and incident response.

## 11. Observability and Operations

- [ ] Add structured logging for invoice generation, payment attempts, retries, and gateway responses.
- [ ] Add OpenTelemetry tracing for request, invoice, payment, and database operations.
- [ ] Add metrics named `invoice_created_total`, `payment_success_total`, `payment_failure_total`, `payment_retry_total`, and `subscription_churn_total`.
- [ ] Create health checks for the API process, SQL Server database, and payment gateway dependency.
- [ ] Add endpoint `/health/live` for liveness and `/health/ready` for readiness.
- [ ] Create dashboard documentation for monitoring billing volume, payment success rate, retry rate, and overdue invoices.

## 12. API Quality and Developer Experience

- [ ] Add OpenAPI metadata, summaries, response types, and examples for every endpoint.
- [ ] Add consistent error response examples for validation errors, idempotency conflicts, missing resources, and gateway failures.
- [ ] Add pagination request parameters to list endpoints.
- [ ] Add sorting and filtering to invoice, customer, subscription, and payment list endpoints.
- [ ] Add API versioning under `/v1` before the first market release.
- [ ] Create a Postman collection or HTTP file with happy-path and failure-path examples.

## 13. Testing and Release Gates

- [ ] Add unit tests for services that calculate invoice totals, payment results, usage totals, and proration.
- [ ] Add integration tests that use SQL Server or a production-compatible database provider instead of only in-memory storage.
- [ ] Add concurrency tests for invoice generation and payment idempotency.
- [ ] Add contract tests for public API request and response payloads.
- [ ] Add a CI workflow that runs restore, build, test, formatting, and migration checks on every pull request.
- [ ] Add a release checklist that requires passing tests, reviewed migrations, configured secrets, and rollback instructions.

## 14. Documentation and Market Launch

- [ ] Update `README.md` with product overview, setup steps, configuration, local run commands, and test commands.
- [ ] Create `docs/api.md` with endpoint descriptions, request examples, and response examples.
- [ ] Create `docs/operations.md` with deployment, migration, monitoring, and rollback instructions.
- [ ] Create `docs/billing-rules.md` that explains recurring billing, usage billing, proration, retries, and dunning rules.
- [ ] Create `docs/security.md` that explains authentication, authorization, PCI boundaries, and data retention.
- [ ] Create sample market demo data for one customer, one product, one recurring plan, one usage plan, one invoice, and one successful payment.
