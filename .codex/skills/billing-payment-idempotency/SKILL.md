---
name: billing-payment-idempotency
description: Check payment-related code changes in the Billing repo for idempotency correctness. Use when adding or reviewing payments, payment attempts, payment retries, charging gateways, invoice payment endpoints, Idempotency-Key handling, or any code that could charge money or mutate payment state more than once after retries or concurrent requests.
---

# Billing Payment Idempotency

## Goal

Ensure payment-related changes in `D:\Code\Billing` are safe when clients retry requests, API calls race, jobs rerun, or external gateway responses arrive more than once.

## Required Review Steps

1. Inspect the changed payment flow end to end:
   - endpoint or background job entry point
   - service method
   - EF entities and indexes
   - SQL Server script updates under `db/sqlserver`
   - tests under `tests/Billing.Api.Tests`

2. Identify the idempotency boundary:
   - Client-triggered payment: require `Idempotency-Key`.
   - Billing-period invoice generation: use natural key `SubscriptionId + BillingPeriodStart + BillingPeriodEnd`.
   - Payment retry job: use natural key such as `PaymentId + AttemptNumber` or scheduled attempt id.
   - Gateway webhook: use provider event id or provider charge id.

3. Verify persistence:
   - Idempotency state must be stored in the database, not only memory.
   - Add a unique index for the idempotency key or natural key.
   - Store enough response/result data to return the same outcome on retry.
   - Store request hash or request shape when using `Idempotency-Key`; same key with different body must fail.

4. Verify concurrency behavior:
   - Check-then-insert must be protected by a unique constraint.
   - In-process locks are acceptable only as an optimization, never as the sole correctness guard.
   - Catch unique constraint conflicts and return the existing result when the conflict is an idempotent duplicate.

5. Verify payment safety:
   - Never call the payment gateway twice for the same idempotency key.
   - Persist a pending/in-progress idempotency record before calling the gateway.
   - Clearly handle pending, succeeded, failed, and unknown gateway outcomes.
   - Do not mark an invoice paid unless the payment succeeded.
   - Do not create multiple successful `Payment` rows for the same invoice charge.

6. Verify retry behavior:
   - Retrying a failed payment attempt must create a new attempt only according to retry schedule.
   - Retrying the same HTTP request must not create a new scheduled attempt.
   - Final failure should transition subscription/invoice state exactly once.

7. Verify tests:
   - Duplicate request with same `Idempotency-Key` returns same result.
   - Same key with different request body is rejected.
   - Concurrent duplicate requests create one charge/payment.
   - Missing `Idempotency-Key` is rejected for payment endpoints.
   - Failed gateway call behavior is explicitly tested.

## Billing Repo Patterns

Use existing structure unless the feature has outgrown it:

- API routes: `src/Billing.Api/Endpoints`
- Business logic: `src/Billing.Api/Services`
- Entities/constants: `src/Billing.Api/Domain`
- EF mapping: `src/Billing.Api/Data/BillingDbContext.cs`
- SQL reference schema: `db/sqlserver/001_initial_schema.sql`
- Integration tests: `tests/Billing.Api.Tests`

Prefer feature naming over generic folders. For example, if payment grows, use `Services/Payments/` rather than `Services/Interfaces/` and `Services/Implementations/`.

## Expected Output

When reviewing, lead with findings:

- Severity
- File and line
- Why the issue can duplicate charges or corrupt payment state
- Concrete fix

If implementing, finish with:

- Files changed
- Idempotency key used
- Database constraints added
- Tests added
- Test command result
