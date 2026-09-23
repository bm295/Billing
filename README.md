# Billing

SaaS Billing Engine API viết bằng Java 25, Spring Boot, Spring Data JPA và SQL Server.

## Cấu trúc

- `src/main/java/com/example/billing/api` — HTTP endpoints và JSON contracts
- `src/main/java/com/example/billing/service` — nghiệp vụ catalog, customer, subscription, invoice, payment và usage
- `src/main/java/com/example/billing/domain` — JPA entities và domain constants
- `src/main/java/com/example/billing/persistence` — repositories và seed data
- `src/test/java` — integration/behavior tests trên H2 ở chế độ SQL Server
- `db/sqlserver/001_initial_schema.sql` — schema SQL Server tham chiếu

## Yêu cầu

- JDK 25
- Maven 3.9+
- SQL Server với schema trong `db/sqlserver/001_initial_schema.sql`

## Chạy

Thiết lập `BILLING_DB_URL`, `BILLING_DB_USERNAME`, `BILLING_DB_PASSWORD`, sau đó:

```powershell
mvn spring-boot:run
```

Ví dụ URL JDBC: `jdbc:sqlserver://localhost:1433;databaseName=Billing;encrypt=true;trustServerCertificate=true`.

## Kiểm thử và đóng gói

```powershell
mvn test
mvn package
java -jar target/billing-1.0.0.jar
```

API routes và JSON contracts được giữ tương thích với bản C# trước đây. Payment endpoint vẫn bắt buộc `Idempotency-Key`; trạng thái idempotency, request hash và unique constraints được lưu trong database.
