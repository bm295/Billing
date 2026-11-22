package com.example.billing.api;

import com.example.billing.service.InvoiceService;
import com.example.billing.service.OllamaClient;
import com.fasterxml.jackson.core.JsonProcessingException;
import com.fasterxml.jackson.databind.ObjectMapper;
import java.util.UUID;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.*;

@RestController
public class InvoiceExplanationController {
  private final InvoiceService invoices;
  private final OllamaClient llm;
  private final ObjectMapper json;

  public InvoiceExplanationController(InvoiceService invoices, OllamaClient llm, ObjectMapper json) {
    this.invoices = invoices;
    this.llm = llm;
    this.json = json;
  }

  @PostMapping("/invoices/{id}/explanation")
  public ResponseEntity<?> explain(@PathVariable UUID id) throws JsonProcessingException {
    var invoice = invoices.get(id);
    if (invoice == null) {
      return ResponseEntity.status(404)
          .body(new Contracts.ApiError("invoice_not_found", "Invoice does not exist."));
    }
    // Send only billing facts: no customer identifiers, contact details, or payment credentials.
    var facts = json.createObjectNode();
    facts.put("status", invoice.status);
    facts.put("amountDue", invoice.amountDue);
    facts.put("amountPaid", invoice.amountPaid);
    facts.put("billingPeriodStart", invoice.billingPeriodStart.toString());
    facts.put("billingPeriodEnd", invoice.billingPeriodEnd.toString());
    facts.put("dueDate", invoice.dueDate.toString());
    var lines = facts.putArray("lines");
    for (var line : invoice.lines) {
      lines.addObject().put("description", line.description).put("quantity", line.quantity)
          .put("unitPrice", line.unitPrice).put("amount", line.amount);
    }
    try {
      var explanation = llm.explain(json.writeValueAsString(facts));
      return ResponseEntity.ok(new ExplanationResponse(id, llm.model(), explanation, true));
    } catch (OllamaClient.Unavailable e) {
      return ResponseEntity.status(503).body(new Contracts.ApiError("llm_unavailable",
          "Invoice explanation is unavailable. Check Ollama and the configured model, then retry."));
    }
  }

  public record ExplanationResponse(UUID invoiceId, String model, String explanation, boolean aiGenerated) {}
}
