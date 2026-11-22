package com.example.billing;

import com.example.billing.api.ChatController;
import com.example.billing.persistence.Repositories;
import com.example.billing.service.OllamaClient;
import java.time.Clock;
import org.springframework.boot.SpringApplication;
import org.springframework.boot.SpringBootConfiguration;
import org.springframework.boot.autoconfigure.EnableAutoConfiguration;
import org.springframework.context.annotation.*;
import org.springframework.data.jpa.repository.config.EnableJpaRepositories;

@SpringBootConfiguration
@EnableAutoConfiguration
@Import({BillingApplication.BillingConfiguration.class, BillingApplication.ChatConfiguration.class})
public class BillingApplication {
  public static void main(String[] args) { SpringApplication.run(BillingApplication.class, args); }
  @Bean Clock clock() { return Clock.systemUTC(); }

  @Configuration(proxyBeanMethods = false)
  @Profile("!chat")
  @ComponentScan({"com.example.billing.api", "com.example.billing.service", "com.example.billing.persistence"})
  @EnableJpaRepositories(basePackageClasses = Repositories.class, considerNestedRepositories = true)
  static class BillingConfiguration {}

  @Configuration(proxyBeanMethods = false)
  @Profile("chat")
  @Import({ChatController.class, OllamaClient.class})
  static class ChatConfiguration {}
}
