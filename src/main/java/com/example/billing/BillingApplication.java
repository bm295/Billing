package com.example.billing;
import com.example.billing.persistence.Repositories; import java.time.Clock; import org.springframework.boot.SpringApplication; import org.springframework.boot.autoconfigure.SpringBootApplication; import org.springframework.context.annotation.Bean; import org.springframework.data.jpa.repository.config.EnableJpaRepositories;
@SpringBootApplication @EnableJpaRepositories(basePackageClasses=Repositories.class,considerNestedRepositories=true)
public class BillingApplication { public static void main(String[] args){SpringApplication.run(BillingApplication.class,args);} @Bean Clock clock(){return Clock.systemUTC();} }
