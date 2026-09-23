package com.example.billing.service;
import java.util.concurrent.*; import java.util.concurrent.locks.*; import org.springframework.stereotype.Component;
@Component public class KeyedLock { private final ConcurrentMap<String,ReentrantLock> locks=new ConcurrentHashMap<>(); public AutoCloseable acquire(String key){var lock=locks.computeIfAbsent(key,k->new ReentrantLock());lock.lock();return lock::unlock;} }
