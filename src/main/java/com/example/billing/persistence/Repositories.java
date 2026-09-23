package com.example.billing.persistence;

import com.example.billing.domain.*;
import java.time.LocalDate;
import java.time.OffsetDateTime;
import java.util.List;
import java.util.Optional;
import java.util.UUID;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.Query;
import org.springframework.stereotype.Repository;

public final class Repositories {
  private Repositories() {}

  @Repository
  public interface CustomerRepository extends JpaRepository<Customer, UUID> {
    boolean existsByEmail(String email);
    List<Customer> findAllByOrderByCreatedAtAsc();
  }

  @Repository
  public interface ProductRepository extends JpaRepository<Product, UUID> {
    List<Product> findAllByOrderByNameAscIdAsc();
  }

  @Repository
  public interface PricePlanRepository extends JpaRepository<PricePlan, UUID> {
    Optional<PricePlan> findByIdAndActiveTrue(UUID id);
    List<PricePlan> findAllByOrderByProductIdAscBillingTypeAscIdAsc();
  }

  @Repository
  public interface SubscriptionRepository extends JpaRepository<Subscription, UUID> {
    List<Subscription> findByCustomerIdOrderByStartDateAscIdAsc(UUID id);

    @Query("select s.id as subscriptionId, "
        + "coalesce(sum(case when a.attemptNumber > 1 then 1 else 0 end), 0) as retryCount "
        + "from Subscription s "
        + "left join Invoice i on i.subscriptionId = s.id "
        + "left join Payment p on p.invoiceId = i.id "
        + "left join PaymentAttempt a on a.payment.id = p.id "
        + "group by s.id")
    List<SubscriptionRetryCount> findSubscriptionRetryCounts();
  }

  public interface SubscriptionRetryCount {
    UUID getSubscriptionId();
    Long getRetryCount();
  }

  @Repository
  public interface InvoiceRepository extends JpaRepository<Invoice, UUID> {
    Optional<Invoice> findBySubscriptionIdAndBillingPeriodStartAndBillingPeriodEnd(
        UUID id, LocalDate start, LocalDate end);
    List<Invoice> findByCustomerIdOrderByCreatedAtDescIdDesc(UUID id);
  }

  @Repository public interface PaymentRepository extends JpaRepository<Payment, UUID> {}
  @Repository public interface IdempotencyRecordRepository
      extends JpaRepository<IdempotencyRecord, UUID> {
    Optional<IdempotencyRecord> findByKey(String key);
  }

  @Repository
  public interface UsageRecordRepository extends JpaRepository<UsageRecord, UUID> {
    Optional<UsageRecord> findByIdempotencyKey(String key);
    List<UsageRecord>
        findBySubscriptionIdAndTimestampGreaterThanEqualAndTimestampLessThanOrderByTimestampAscIdAsc(
            UUID id, OffsetDateTime start, OffsetDateTime end);
  }
}
