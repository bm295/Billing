package com.example.billing.api;

import com.example.billing.service.OllamaClient;
import java.util.List;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.*;

@RestController
public class ChatController {
  private final OllamaClient llm;
  public ChatController(OllamaClient llm) { this.llm = llm; }

  @PostMapping("/api/chat")
  public ResponseEntity<?> chat(@RequestBody ChatRequest request) {
    var messages = request.messages();
    if (messages == null || messages.isEmpty() || messages.size() > 40
        || messages.stream().anyMatch(m -> m == null || m.content() == null
            || m.content().isBlank() || m.content().length() > 8000)
        || messages.stream().mapToInt(m -> m.content().length()).sum() > 32000) {
      return invalid();
    }
    for (int i = 0; i < messages.size(); i++) {
      if (!(i % 2 == 0 ? "user" : "assistant").equals(messages.get(i).role())) return invalid();
    }
    if (!"user".equals(messages.getLast().role())) return invalid();
    try {
      return ResponseEntity.ok(new ChatResponse(llm.model(), llm.chat(messages)));
    } catch (OllamaClient.Unavailable e) {
      return ResponseEntity.status(503).body(new Contracts.ApiError("llm_unavailable",
          "Ollama is unavailable. Check the server and configured model."));
    }
  }

  private ResponseEntity<?> invalid() {
    return ResponseEntity.badRequest().body(new Contracts.ApiError("invalid_messages",
        "Use alternating user/assistant messages ending with user; at most 40 messages, 8000 characters each and 32000 total."));
  }
  public record ChatRequest(List<OllamaClient.ChatMessage> messages) {}
  public record ChatResponse(String model, String reply) {}
}
