# Operations Runbook

## Triển khai schema trước API

Ứng dụng dùng `spring.jpa.hibernate.ddl-auto=validate`: nó kiểm tra schema lúc khởi động nhưng không tự thay đổi production database. Hãy review và chạy `db/sqlserver/001_initial_schema.sql` bằng công cụ triển khai SQL chuẩn của tổ chức trước khi rollout API.

Trước khi chạy script, xác nhận backup gần nhất, quyền tạo/thay đổi schema và kết nối tới SQL Server. Sau khi chạy, kiểm tra các unique index quan trọng:

- `IX_Invoices_SubscriptionId_BillingPeriod`
- `IX_Payments_IdempotencyKey`
- `IX_Payments_InvoiceId_Succeeded`
- `IX_PaymentAttempts_PaymentId_AttemptNumber`
- `IX_IdempotencyRecords_Key`
- `IX_UsageRecords_IdempotencyKey`

## Cấu hình runtime

Thiết lập `BILLING_DB_URL`, `BILLING_DB_USERNAME`, `BILLING_DB_PASSWORD`, rồi build và kiểm tra artifact:

```bash
mvn --batch-mode clean verify
java -jar target/billing-1.0.0.jar
```

Chỉ rollout API sau khi schema validation thành công. Nếu script database lỗi, dừng rollout và khôi phục từ backup khi database ở trạng thái không an toàn.
