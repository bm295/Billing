package com.example.billing.api;

import com.example.billing.service.RetryNeighborAnalyzer;
import com.example.billing.service.SubscriptionRetryAnalysisService;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

@RestController
@RequestMapping("/analytics/subscriptions")
public class AnalyticsController {
  private final SubscriptionRetryAnalysisService service;

  public AnalyticsController(SubscriptionRetryAnalysisService service) {
    this.service = service;
  }

  @GetMapping("/retry-neighbors")
  RetryNeighborAnalyzer.Result analyzeRetryNeighbors() {
    return service.analyze();
  }
}
