package com.example.billing;

import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.post;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.jsonPath;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.status;

import com.example.billing.api.ApiExceptionHandler;
import com.example.billing.api.CollectionQueueController;
import org.junit.jupiter.api.Test;
import org.springframework.http.MediaType;
import org.springframework.test.web.servlet.setup.MockMvcBuilders;

class CollectionQueuePositionApiTests {
  @Test
  void previewsPositionAndReportsInvalidRequests() throws Exception {
    var mvc = MockMvcBuilders.standaloneSetup(new CollectionQueueController())
        .setControllerAdvice(new ApiExceptionHandler()).build();

    mvc.perform(post("/analytics/collection-queue/position-preview")
            .contentType(MediaType.APPLICATION_JSON)
            .content("""
                {"queueSize":5,"originalPosition":1,"reversalStarts":[0,1,2,3,4]}
                """))
        .andExpect(status().isOk())
        .andExpect(jsonPath("$.finalPosition").value(3));

    for (String body : new String[] {
        "{}", "{\"queueSize\":5,\"originalPosition\":5,\"reversalStarts\":[]}",
        "{\"queueSize\":5,\"originalPosition\":0,\"reversalStarts\":[null]}"}) {
      mvc.perform(post("/analytics/collection-queue/position-preview")
              .contentType(MediaType.APPLICATION_JSON).content(body))
          .andExpect(status().isBadRequest())
          .andExpect(jsonPath("$.code").value("invalid_request"));
    }
  }
}
