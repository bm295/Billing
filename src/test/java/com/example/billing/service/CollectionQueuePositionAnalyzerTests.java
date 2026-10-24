package com.example.billing.service;

import static org.assertj.core.api.Assertions.assertThat;
import static org.assertj.core.api.Assertions.assertThatThrownBy;

import java.util.ArrayList;
import java.util.Arrays;
import java.util.Collections;
import java.util.List;
import java.util.Random;
import java.util.stream.IntStream;
import org.junit.jupiter.api.Test;

class CollectionQueuePositionAnalyzerTests {
  @Test
  void matchesActualQueueReversalsForOriginalAndCustomSequences() {
    var random = new Random(42);
    for (int size = 1; size <= 30; size++) {
      var sequential = IntStream.range(0, size).boxed().toList();
      var custom = new ArrayList<Integer>();
      for (int step = 0; step < 40; step++) custom.add(random.nextInt(size));
      for (var starts : List.of(sequential, custom)) {
        var queue = new ArrayList<>(IntStream.range(0, size).boxed().toList());
        for (int start : starts) Collections.reverse(queue.subList(start, size));
        for (int original = 0; original < size; original++) {
          assertThat(CollectionQueuePositionAnalyzer.preview(size, original, starts))
              .isEqualTo(queue.indexOf(original));
        }
      }
    }
  }

  @Test
  void handlesNoChangesAndLargeQueuesWithoutOverflow() {
    assertThat(CollectionQueuePositionAnalyzer.preview(5, 2, List.of())).isEqualTo(2);
    assertThat(CollectionQueuePositionAnalyzer.preview(1, 0, List.of(0))).isZero();
    assertThat(CollectionQueuePositionAnalyzer.preview(
        Integer.MAX_VALUE, Integer.MAX_VALUE - 1, List.of(Integer.MAX_VALUE - 2)))
        .isEqualTo(Integer.MAX_VALUE - 2);
  }

  @Test
  void rejectsInvalidQueueDescriptions() {
    assertInvalid(null, 0, List.of());
    assertInvalid(0, 0, List.of());
    assertInvalid(-1, 0, List.of());
    assertInvalid(5, null, List.of());
    assertInvalid(5, -1, List.of());
    assertInvalid(5, 5, List.of());
    assertInvalid(5, 0, null);
    assertInvalid(5, 0, List.of(-1));
    assertInvalid(5, 0, List.of(5));
    assertInvalid(5, 0, Arrays.asList(0, null));
  }

  private void assertInvalid(Integer size, Integer position, List<Integer> starts) {
    assertThatThrownBy(() -> CollectionQueuePositionAnalyzer.preview(size, position, starts))
        .isInstanceOf(IllegalArgumentException.class);
  }
}
