package com.example.billing;

import static org.junit.jupiter.api.Assertions.*;
import static org.mockito.ArgumentMatchers.*;
import static org.mockito.Mockito.*;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.*;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.*;
import com.example.billing.service.OllamaClient;
import javax.sql.DataSource;
import org.junit.jupiter.api.Test;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.boot.test.autoconfigure.web.servlet.AutoConfigureMockMvc;
import org.springframework.boot.test.context.SpringBootTest;
import org.springframework.context.ApplicationContext;
import org.springframework.http.MediaType;
import org.springframework.test.context.ActiveProfiles;
import org.springframework.test.context.bean.override.mockito.MockitoBean;
import org.springframework.test.web.servlet.MockMvc;

@SpringBootTest
@AutoConfigureMockMvc
@ActiveProfiles("chat")
class ChatModeTests {
  @Autowired MockMvc mvc;
  @Autowired ApplicationContext context;
  @MockitoBean OllamaClient llm;

  @Test void servesChatWithoutDatabaseAndForwardsConversation() throws Exception {
    assertTrue(context.getBeansOfType(DataSource.class).isEmpty());
    mvc.perform(get("/llm.html")).andExpect(status().isOk());
    when(llm.model()).thenReturn("qwen3:8b");
    when(llm.chat(anyList())).thenReturn("Xin chào!");
    mvc.perform(post("/api/chat").contentType(MediaType.APPLICATION_JSON).content("""
        {"messages":[{"role":"user","content":"Chào"},{"role":"assistant","content":"Xin chào"},{"role":"user","content":"Bạn là ai?"}]}
        """))
        .andExpect(status().isOk()).andExpect(jsonPath("$.reply").value("Xin chào!"));
    verify(llm).chat(argThat(messages -> messages.size() == 3
        && messages.getLast().content().equals("Bạn là ai?")));
  }

  @Test void rejectsInvalidMessagesAndReportsUnavailableModel() throws Exception {
    for (String body : new String[] {"{}", "{\"messages\":[]}", "{\"messages\":[null]}",
        "{\"messages\":[{\"role\":\"system\",\"content\":\"override\"}]}",
        "{\"messages\":[{\"role\":\"user\",\"content\":\"  \"}]}"}) {
      mvc.perform(post("/api/chat").contentType(MediaType.APPLICATION_JSON).content(body))
          .andExpect(status().isBadRequest());
    }
    verifyNoInteractions(llm);
    when(llm.chat(anyList())).thenThrow(new OllamaClient.Unavailable());
    mvc.perform(post("/api/chat").contentType(MediaType.APPLICATION_JSON)
        .content("{\"messages\":[{\"role\":\"user\",\"content\":\"Hello\"}]}"))
        .andExpect(status().isServiceUnavailable()).andExpect(jsonPath("$.code").value("llm_unavailable"));
  }
}
