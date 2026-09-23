package com.example.billing.service;

import static org.assertj.core.api.Assertions.assertThat;

import java.util.List;
import org.junit.jupiter.api.Test;

class RetryNeighborAnalyzerTests {
  @Test
  void findsLargestPairOfAdjacentRetryCounts() {
    var result = RetryNeighborAnalyzer.analyze(List.of(1, 1, 2, 2, 2, 4));

    assertThat(result.lowerRetryCount()).isEqualTo(1);
    assertThat(result.upperRetryCount()).isEqualTo(2);
    assertThat(result.subscriptionCount()).isEqualTo(5);
  }

  @Test
  void supportsZeroAndValuesAboveOneHundred() {
    var result = RetryNeighborAnalyzer.analyze(List.of(100, 101, 101, 250));

    assertThat(result.lowerRetryCount()).isEqualTo(100);
    assertThat(result.upperRetryCount()).isEqualTo(101);
    assertThat(result.subscriptionCount()).isEqualTo(3);
  }

  @Test
  void choosesLowerPairWhenGroupsTie() {
    var result = RetryNeighborAnalyzer.analyze(List.of(0, 1, 3, 4));

    assertThat(result.lowerRetryCount()).isZero();
    assertThat(result.upperRetryCount()).isEqualTo(1);
  }

  @Test
  void returnsEmptyAnalysisWhenThereAreNoSubscriptions() {
    var result = RetryNeighborAnalyzer.analyze(List.of());

    assertThat(result.subscriptionCount()).isZero();
    assertThat(result.lowerRetryCount()).isNull();
    assertThat(result.upperRetryCount()).isNull();
  }
}
