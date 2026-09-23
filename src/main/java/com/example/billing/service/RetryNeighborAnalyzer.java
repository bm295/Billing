package com.example.billing.service;

import java.util.List;
import java.util.Map;
import java.util.TreeMap;

public final class RetryNeighborAnalyzer {
  private RetryNeighborAnalyzer() {}

  public static Result analyze(List<Integer> retryCounts) {
    if (retryCounts.isEmpty()) {
      return new Result(null, null, 0);
    }

    Map<Integer, Integer> frequencies = new TreeMap<>();
    for (int retryCount : retryCounts) {
      if (retryCount < 0) {
        throw new IllegalArgumentException("Retry count cannot be negative.");
      }
      frequencies.merge(retryCount, 1, Integer::sum);
    }

    int bestLowerBound = frequencies.keySet().iterator().next();
    int largestGroup = 0;
    for (var entry : frequencies.entrySet()) {
      int lowerBound = entry.getKey();
      int groupSize = entry.getValue() + frequencies.getOrDefault(lowerBound + 1, 0);
      if (groupSize > largestGroup) {
        bestLowerBound = lowerBound;
        largestGroup = groupSize;
      }
    }

    return new Result(bestLowerBound, bestLowerBound + 1, largestGroup);
  }

  public record Result(Integer lowerRetryCount, Integer upperRetryCount, int subscriptionCount) {}
}
