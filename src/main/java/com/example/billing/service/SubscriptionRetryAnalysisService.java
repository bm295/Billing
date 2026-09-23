package com.example.billing.service;

import com.example.billing.persistence.Repositories.SubscriptionRepository;
import java.util.List;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

@Service
public class SubscriptionRetryAnalysisService {
  private final SubscriptionRepository subscriptions;

  public SubscriptionRetryAnalysisService(SubscriptionRepository subscriptions) {
    this.subscriptions = subscriptions;
  }

  @Transactional(readOnly = true)
  public RetryNeighborAnalyzer.Result analyze() {
    List<Integer> retryCounts = subscriptions.findSubscriptionRetryCounts().stream()
        .map(row -> Math.toIntExact(row.getRetryCount()))
        .toList();
    return RetryNeighborAnalyzer.analyze(retryCounts);
  }
}
