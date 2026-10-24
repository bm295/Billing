package com.example.billing.api;

import com.example.billing.service.CollectionQueuePositionAnalyzer;
import java.util.List;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RestController;

@RestController
public class CollectionQueueController {
  public record PositionPreviewRequest(
      Integer queueSize, Integer originalPosition, List<Integer> reversalStarts) {}

  public record PositionPreviewResponse(int finalPosition) {}

  @PostMapping("/analytics/collection-queue/position-preview")
  PositionPreviewResponse preview(@RequestBody PositionPreviewRequest request) {
    return new PositionPreviewResponse(CollectionQueuePositionAnalyzer.preview(
        request.queueSize(), request.originalPosition(), request.reversalStarts()));
  }
}
