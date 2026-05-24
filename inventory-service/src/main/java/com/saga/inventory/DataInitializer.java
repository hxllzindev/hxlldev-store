package com.saga.inventory;

import com.saga.inventory.model.InventoryItem;
import com.saga.inventory.repository.InventoryRepository;
import org.springframework.boot.CommandLineRunner;
import org.springframework.stereotype.Component;

@Component
public class DataInitializer implements CommandLineRunner {

    private final InventoryRepository repository;

    public DataInitializer(InventoryRepository repository) {
        this.repository = repository;
    }

    @Override
    public void run(String... args) {
        if (!repository.existsById("abc")) {
            repository.save(new InventoryItem("abc", 100));
            System.out.println("📦 Produto 'abc' cadastrado com 100 unidades");
        }
    }
}
