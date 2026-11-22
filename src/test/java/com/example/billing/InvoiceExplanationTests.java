package com.example.billing;

import static org.junit.jupiter.api.Assertions.*;
import static org.mockito.ArgumentMatchers.anyString;
import static org.mockito.Mockito.*;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.post;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.*;

import com.example.billing.api.InvoiceExplanationController;
import com.example.billing.domain.Invoice;
import com.example.billing.domain.InvoiceLine;
import com.example.billing.service.InvoiceService;
import com.example.billing.service.OllamaClient;
import com.fasterxml.jackson.databind.ObjectMapper;
import com.sun.net.httpserver.HttpServer;
import java.math.BigDecimal;
import java.net.InetSocketAddress;
import java.nio.charset.StandardCharsets;
import java.time.LocalDate;
import java.util.UUID;
import java.util.concurrent.atomic.AtomicReference;
import org.junit.jupiter.api.Test;
import org.springframework.test.web.servlet.setup.MockMvcBuilders;

class InvoiceExplanationTests {
  @Test
  void explainsStoredFactsWithoutSendingCustomerIdentifiers() throws Exception {
    var invoices = mock(InvoiceService.class);
    var llm = mock(OllamaClient.class);
    var invoice = new Invoice();
    invoice.id = UUID.randomUUID();
    invoice.customerId = UUID.randomUUID();
    invoice.subscriptionId = UUID.randomUUID();
    invoice.status = "Open";
    invoice.amountDue = new BigDecimal("100.00");
    invoice.amountPaid = BigDecimal.ZERO;
    invoice.billingPeriodStart = LocalDate.of(2026, 11, 1);
    invoice.billingPeriodEnd = LocalDate.of(2026, 12, 1);
    invoice.dueDate = invoice.billingPeriodEnd;
    var line = new InvoiceLine();
    line.description = "Monthly plan";
    line.quantity = BigDecimal.ONE;
    line.unitPrice = invoice.amountDue;
    line.amount = invoice.amountDue;
    invoice.lines.add(line);
    when(invoices.get(invoice.id)).thenReturn(invoice);
    when(llm.model()).thenReturn("qwen3:8b");
    when(llm.explain(anyString())).thenAnswer(call -> {
      var payload = call.getArgument(0, String.class);
      var facts = new ObjectMapper().readTree(payload);
      assertEquals(100, facts.get("amountDue").intValue());
      assertEquals("Monthly plan", facts.get("lines").get(0).get("description").asText());
      assertFalse(payload.contains(invoice.customerId.toString()));
      assertFalse(payload.contains(invoice.subscriptionId.toString()));
      return "Your monthly plan invoice is open.";
    });
    var mvc = MockMvcBuilders.standaloneSetup(
        new InvoiceExplanationController(invoices, llm, new ObjectMapper())).build();
    mvc.perform(post("/invoices/{id}/explanation", invoice.id))
        .andExpect(status().isOk())
        .andExpect(jsonPath("$.invoiceId").value(invoice.id.toString()))
        .andExpect(jsonPath("$.aiGenerated").value(true))
        .andExpect(jsonPath("$.model").value("qwen3:8b"))
        .andExpect(jsonPath("$.explanation").value("Your monthly plan invoice is open."));
    verify(invoices, never()).generate(any());

    doThrow(new OllamaClient.Unavailable()).when(llm).explain(anyString());
    mvc.perform(post("/invoices/{id}/explanation", invoice.id))
        .andExpect(status().isServiceUnavailable())
        .andExpect(jsonPath("$.code").value("llm_unavailable"));

    clearInvocations(llm);
    mvc.perform(post("/invoices/{id}/explanation", UUID.randomUUID()))
        .andExpect(status().isNotFound())
        .andExpect(jsonPath("$.code").value("invoice_not_found"));
    verifyNoInteractions(llm);
  }

  @Test
  void callsOllamaAndRejectsFailedEmptyOrTruncatedResponses() throws Exception {
    var response = new AtomicReference<>("""
        {"message":{"content":"Invoice explanation"},"done":true,"done_reason":"stop"}
        """);
    var request = new AtomicReference<String>();
    var statusCode = new java.util.concurrent.atomic.AtomicInteger(200);
    var server = HttpServer.create(new InetSocketAddress("127.0.0.1", 0), 0);
    server.createContext("/api/chat", exchange -> {
      request.set(new String(exchange.getRequestBody().readAllBytes(), StandardCharsets.UTF_8));
      var bytes = response.get().getBytes(StandardCharsets.UTF_8);
      exchange.getResponseHeaders().set("Content-Type", "application/json");
      exchange.sendResponseHeaders(statusCode.get(), bytes.length);
      try (var output = exchange.getResponseBody()) { output.write(bytes); }
    });
    server.start();
    try {
      var client = new OllamaClient("http://127.0.0.1:" + server.getAddress().getPort(), "qwen3:8b", 2);
      assertEquals("Invoice explanation", client.explain("{\"status\":\"Open\"}"));
      var body = new ObjectMapper().readTree(request.get());
      assertEquals("qwen3:8b", body.get("model").asText());
      assertFalse(body.get("stream").asBoolean());
      assertFalse(body.get("think").asBoolean());
      assertEquals("system", body.get("messages").get(0).get("role").asText());
      assertEquals("{\"status\":\"Open\"}", body.get("messages").get(1).get("content").asText());
      assertEquals("Invoice explanation", client.chat(java.util.List.of(
          new OllamaClient.ChatMessage("user", "Hello"),
          new OllamaClient.ChatMessage("assistant", "Hi"),
          new OllamaClient.ChatMessage("user", "Explain LLMs"))));
      var chatBody = new ObjectMapper().readTree(request.get());
      assertEquals(3, chatBody.get("messages").size());
      assertEquals("Explain LLMs", chatBody.get("messages").get(2).get("content").asText());
      assertEquals("user", chatBody.get("messages").get(0).get("role").asText());
      for (var invalid : new String[] {"{}", "not json",
          "{\"message\":{\"content\":\" \"},\"done\":true}",
          "{\"message\":{\"content\":\"partial\"},\"done\":false}",
          "{\"message\":{\"content\":\"partial\"},\"done\":true,\"done_reason\":\"length\"}"}) {
        response.set(invalid);
        assertThrows(OllamaClient.Unavailable.class, () -> client.explain("{}"));
      }
      statusCode.set(500);
      assertThrows(OllamaClient.Unavailable.class, () -> client.explain("{}"));
    } finally {
      server.stop(0);
    }
  }
}
