package com.example.billing;

import static com.example.billing.persistence.BillingSeedData.PRO_PLAN_ID;
import static com.example.billing.persistence.BillingSeedData.SAMPLE_CUSTOMER_ID;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.get;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.jsonPath;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.status;

import com.example.billing.domain.BillingConstants.InvoiceStatus;
import com.example.billing.domain.BillingConstants.PaymentStatus;
import com.example.billing.domain.BillingConstants.SubscriptionStatus;
import com.example.billing.domain.Invoice;
import com.example.billing.domain.Payment;
import com.example.billing.domain.PaymentAttempt;
import com.example.billing.domain.Subscription;
import com.example.billing.persistence.Repositories.PaymentRepository;
import com.example.billing.persistence.Repositories.SubscriptionRepository;
import com.example.billing.persistence.Repositories.InvoiceRepository;
import java.math.BigDecimal;
import java.time.LocalDate;
import java.time.OffsetDateTime;
import java.util.UUID;
import org.junit.jupiter.api.Test;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.boot.test.autoconfigure.web.servlet.AutoConfigureMockMvc;
import org.springframework.boot.test.context.SpringBootTest;
import org.springframework.test.annotation.DirtiesContext;
import org.springframework.test.web.servlet.MockMvc;

@SpringBootTest
@AutoConfigureMockMvc
@DirtiesContext(classMode = DirtiesContext.ClassMode.BEFORE_CLASS)
class SubscriptionRetryAnalysisIntegrationTests {
  @Autowired MockMvc mvc;
  @Autowired SubscriptionRepository subscriptions;
  @Autowired InvoiceRepository invoices;
  @Autowired PaymentRepository payments;

  @Test
  void endpointFindsLargestNeighborGroupWithoutMutatingPayments() throws Exception {
    for (int retryCount : new int[] {1, 1, 2, 2, 2, 4}) {
      createSubscriptionWithRetries(retryCount);
    }
    long paymentCountBefore = payments.count();

    mvc.perform(get("/analytics/subscriptions/retry-neighbors"))
        .andExpect(status().isOk())
        .andExpect(jsonPath("$.lowerRetryCount").value(1))
        .andExpect(jsonPath("$.upperRetryCount").value(2))
        .andExpect(jsonPath("$.subscriptionCount").value(5));

    org.assertj.core.api.Assertions.assertThat(payments.count()).isEqualTo(paymentCountBefore);
    org.assertj.core.api.Assertions.assertThat(
        payments.findAll().stream().allMatch(payment -> PaymentStatus.FAILED.equals(payment.status)))
        .isTrue();
  }

  private void createSubscriptionWithRetries(int retryCount) {
    var subscription = new Subscription();
    subscription.id = UUID.randomUUID();
    subscription.customerId = SAMPLE_CUSTOMER_ID;
    subscription.pricePlanId = PRO_PLAN_ID;
    subscription.status = SubscriptionStatus.ACTIVE;
    subscription.startDate = LocalDate.of(2028, 1, 1).plusDays(subscriptions.count());
    subscription.currentPeriodStart = subscription.startDate;
    subscription.currentPeriodEnd = subscription.startDate.plusMonths(1);
    subscriptions.save(subscription);

    var invoice = new Invoice();
    invoice.id = UUID.randomUUID();
    invoice.customerId = SAMPLE_CUSTOMER_ID;
    invoice.subscriptionId = subscription.id;
    invoice.status = InvoiceStatus.OPEN;
    invoice.amountDue = BigDecimal.TEN;
    invoice.amountPaid = BigDecimal.ZERO;
    invoice.billingPeriodStart = subscription.currentPeriodStart;
    invoice.billingPeriodEnd = subscription.currentPeriodEnd;
    invoice.dueDate = subscription.currentPeriodEnd;
    invoice.createdAt = OffsetDateTime.now();
    invoices.save(invoice);

    var payment = new Payment();
    payment.id = UUID.randomUUID();
    payment.invoiceId = invoice.id;
    payment.amount = BigDecimal.TEN;
    payment.status = PaymentStatus.FAILED;
    payment.provider = "test_gateway";
    payment.idempotencyKey = UUID.randomUUID().toString();
    payment.createdAt = OffsetDateTime.now();
    for (int attemptNumber = 1; attemptNumber <= retryCount + 1; attemptNumber++) {
      var attempt = new PaymentAttempt();
      attempt.id = UUID.randomUUID();
      attempt.payment = payment;
      attempt.attemptNumber = attemptNumber;
      attempt.status = PaymentStatus.FAILED;
      attempt.createdAt = OffsetDateTime.now();
      payment.attempts.add(attempt);
    }
    payments.save(payment);
  }
}
