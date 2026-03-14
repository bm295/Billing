# SaaS Billing Engine Requirements

## 1. Tổng quan hệ thống

SaaS Billing Engine là hệ thống quản lý thanh toán định kỳ và theo mức sử dụng cho các sản phẩm SaaS.

### Ví dụ use case

Customer đăng ký gói **Pro: $49/month** + **$0.02 / API call**.

Hệ thống cần:

- Tạo subscription
- Đo usage
- Tính phí
- Tạo invoice
- Charge payment
- Retry nếu payment fail

## 2. Core Domain Model

Các entity quan trọng:

- Customer
- Product
- PricePlan
- Subscription
- UsageRecord
- Invoice
- InvoiceLine
- Payment
- PaymentAttempt
- CreditBalance

### Customer

| Field | Description |
| --- | --- |
| Id | Định danh khách hàng |
| Email | Email khách hàng |
| CompanyName | Tên công ty |
| BillingAddress | Địa chỉ thanh toán |
| PaymentMethodId | Phương thức thanh toán mặc định |
| Status | Trạng thái |
| CreatedAt | Thời điểm tạo |

### Product

Ví dụ: **API Platform**

| Field | Description |
| --- | --- |
| Id | Định danh sản phẩm |
| Name | Tên sản phẩm |
| Description | Mô tả |
| Active | Cờ kích hoạt |

### PricePlan

| Field | Description |
| --- | --- |
| Id | Định danh gói giá |
| ProductId | Tham chiếu Product |
| BillingType | Recurring / Usage |
| Amount | Số tiền |
| Currency | Loại tiền tệ |
| BillingInterval | Month / Year |
| UsageUnit | Đơn vị usage |

Ví dụ:

- Pro Plan
- $49 / month
- + $0.02 / API call

### Subscription

| Field | Description |
| --- | --- |
| Id | Định danh subscription |
| CustomerId | Tham chiếu Customer |
| PricePlanId | Tham chiếu PricePlan |
| Status | Trạng thái |
| StartDate | Ngày bắt đầu |
| CurrentPeriodStart | Bắt đầu chu kỳ hiện tại |
| CurrentPeriodEnd | Kết thúc chu kỳ hiện tại |
| CancelAtPeriodEnd | Huỷ vào cuối kỳ |

## 3. Usage Billing

Khách hàng có thể bị tính phí theo usage.

Ví dụ: **$0.02 / API request**

### UsageRecord

| Field | Description |
| --- | --- |
| Id | Định danh bản ghi usage |
| CustomerId | Tham chiếu Customer |
| SubscriptionId | Tham chiếu Subscription |
| MetricName | Tên metric |
| Quantity | Số lượng |
| Timestamp | Thời gian ghi nhận |

Ví dụ:

- Metric: `API_CALL`
- Quantity: `1200`

## 4. Invoice System

Invoice được tạo mỗi billing cycle.

### Invoice

| Field | Description |
| --- | --- |
| Id | Định danh invoice |
| CustomerId | Tham chiếu Customer |
| SubscriptionId | Tham chiếu Subscription |
| Status | Trạng thái |
| AmountDue | Số tiền cần thanh toán |
| AmountPaid | Số tiền đã thanh toán |
| DueDate | Hạn thanh toán |
| CreatedAt | Thời điểm tạo |

### InvoiceLine

| Field | Description |
| --- | --- |
| Id | Định danh dòng invoice |
| InvoiceId | Tham chiếu Invoice |
| Description | Mô tả |
| Amount | Thành tiền |
| Quantity | Số lượng |
| UnitPrice | Đơn giá |

Ví dụ:

- Pro Plan monthly: `$49`
- API usage (2300 calls): `$46`
- Total: `$95`

## 5. Proration (tính phí khi upgrade giữa chu kỳ)

Ví dụ:

- Basic plan: `$20/month`
- Upgrade → Pro: `$50/month`
- Sau 10 ngày trong tháng

Tính:

- Unused Basic credit
- + Remaining Pro cost

Công thức:

```text
daily_rate = plan_price / days_in_cycle
```

## 6. Payment System

### Payment flow

```text
Invoice created
     ↓
PaymentAttempt
     ↓
Charge payment gateway
     ↓
Success → Invoice Paid
Fail → Retry logic
```

### Payment

| Field | Description |
| --- | --- |
| Id | Định danh payment |
| InvoiceId | Tham chiếu Invoice |
| Amount | Số tiền |
| Status | Trạng thái |
| Provider | Cổng thanh toán |
| CreatedAt | Thời điểm tạo |

## 7. Payment Retry Logic (rất quan trọng)

Ví dụ retry schedule:

- Day 1: first retry
- Day 3: second retry
- Day 7: final retry

Sau đó:

- Subscription → `past_due`

### PaymentAttempt

| Field | Description |
| --- | --- |
| Id | Định danh attempt |
| PaymentId | Tham chiếu Payment |
| AttemptNumber | Số lần thử |
| Status | Trạng thái |
| FailureReason | Lý do thất bại |

## 8. Background Jobs

Một số job cần chạy định kỳ:

- **Billing job (daily):**
  - Check subscriptions
  - Create invoices
- **Usage aggregation:**
  - Aggregate usage records
- **Payment retry job:**
  - Retry failed payments
- **Dunning management:**
  - Send payment reminder email

## 9. Architecture đề xuất

### High-level architecture

```text
                API Gateway
                     │
         ┌───────────┼───────────┐
         │           │           │
    Billing API   Usage API   Payment API
         │           │           │
         └───────────┼───────────┘
                     │
                Message Bus
                     │
                Background Workers
```

## 10. Microservices decomposition

Có thể chia thành:

### Billing Service

Quản lý:

- Products
- Plans
- Subscriptions
- Invoices

### Usage Service

Quản lý:

- Usage ingestion
- Aggregation
- Pricing

### Payment Service

Quản lý:

- Payment methods
- Charges
- Refunds
- Retry

## 11. Saga Flow (rất hợp để luyện kiến trúc)

Invoice payment workflow:

```text
CreateInvoice
      ↓
AttemptPayment
      ↓
PaymentSuccess → MarkInvoicePaid
      ↓
PaymentFailed → ScheduleRetry
```

Nếu retry hết:

- SuspendSubscription

## 12. Database Schema (SQL Server)

Ví dụ bảng:

- Customers
- Subscriptions
- Products
- PricePlans
- UsageRecords
- Invoices
- InvoiceLines
- Payments
- PaymentAttempts

Index quan trọng:

- `IX_Subscription_Customer`
- `IX_UsageRecord_Subscription`
- `IX_Invoice_Status`

## 13. API Design

- Create subscription: `POST /subscriptions`
- Report usage: `POST /usage`
- List invoices: `GET /invoices`
- Pay invoice: `POST /invoices/{id}/pay`

## 14. Idempotency (rất quan trọng)

Payment API phải hỗ trợ `Idempotency-Key`.

Ví dụ:

```http
POST /payments
Idempotency-Key: abc123
```

Nếu retry request → không charge 2 lần.

## 15. Observability

Metrics:

- `invoice_created`
- `payment_success_rate`
- `payment_retry_rate`
- `subscription_churn`

Logs:

- Payment failures
- Billing errors

Tracing:

- `invoice → payment → gateway`

## 16. Security

Quan trọng:

- PCI compliance
- Tokenized payment method
- No raw card storage

## 17. Những feature nâng cao (giống Stripe)

- Credit balance:
  - Customer prepaid credit
- Coupons:
  - Discount %
- Metered billing:
  - Tier pricing
- Tax engine:
  - VAT / sales tax

## 18. Ví dụ workflow hoàn chỉnh

```text
Customer subscribe plan
        ↓
Subscription active
        ↓
Usage records collected
        ↓
Billing cycle ends
        ↓
Invoice generated
        ↓
Payment attempt
        ↓
Success → invoice paid
Fail → retry → suspend subscription
```

## 19. Tech stack đề xuất (.NET)

### Backend

- ASP.NET Core
- EF Core
- SQL Server

### Messaging

- RabbitMQ
- Azure Service Bus

### Background jobs

- Hangfire
- Quartz

### Observability

- OpenTelemetry
- Prometheus
- Grafana
