package com.example.billing.service;

import java.util.List;

/** Tracks an invoice in a proposed collection queue without materializing the queue. */
public final class CollectionQueuePositionAnalyzer {
  private CollectionQueuePositionAnalyzer() {}

  public static int preview(Integer queueSize, Integer originalPosition, List<Integer> reversalStarts) {
    if (queueSize == null || queueSize <= 0) {
      throw new IllegalArgumentException("Queue size must be positive.");
    }
    if (originalPosition == null || originalPosition < 0 || originalPosition >= queueSize) {
      throw new IllegalArgumentException("Original position must be within the queue (zero-based).");
    }
    if (reversalStarts == null) {
      throw new IllegalArgumentException("Reversal starts are required; use an empty list for no changes.");
    }

    int position = originalPosition;
    for (Integer start : reversalStarts) {
      if (start == null || start < 0 || start >= queueSize) {
        throw new IllegalArgumentException("Each reversal start must be within the queue (zero-based).");
      }
      if (position >= start) {
        // Subtract first to avoid overflowing when queueSize is near Integer.MAX_VALUE.
        position = queueSize - 1 - (position - start);
      }
    }
    return position;
  }
}
