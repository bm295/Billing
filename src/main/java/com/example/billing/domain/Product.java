package com.example.billing.domain;
import jakarta.persistence.*; import java.util.UUID;
@Entity @Table(name="Products") public class Product { @Id @Column(name="Id") public UUID id; @Column(name="Name",length=200,nullable=false) public String name; @Column(name="Description",length=500,nullable=false) public String description; @Column(name="Active",nullable=false) public boolean active; }
