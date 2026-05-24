package com.saga.inventory.service;

import com.fasterxml.jackson.databind.JsonNode;
import com.fasterxml.jackson.databind.ObjectMapper;
import com.saga.inventory.model.InventoryItem;
import com.saga.inventory.repository.InventoryRepository;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.kafka.annotation.KafkaListener;
import org.springframework.kafka.core.KafkaTemplate;
import org.springframework.stereotype.Service;

import java.util.Optional;

@Service
public class InventoryService {

    @Autowired
    private InventoryRepository repository;

    @Autowired
    private KafkaTemplate<String, String> kafkaTemplate;

    private final ObjectMapper mapper = new ObjectMapper();

    @KafkaListener(topics = "order-created", groupId = "inventory-group")
    public void handleOrderCreated(String message) {
        try {
            JsonNode node = mapper.readTree(message);
            String orderId = node.get("orderId").asText();
            JsonNode items = node.get("items");

            String productId = items.get(0).get("productId").asText();
            int quantity = items.get(0).get("quantity").asInt();

            Optional<InventoryItem> optional = repository.findById(productId);

            if (optional.isPresent() && optional.get().getAvailableQuantity() >= quantity) {
                InventoryItem item = optional.get();
                item.setAvailableQuantity(item.getAvailableQuantity() - quantity);
                repository.save(item);

                String response = mapper.writeValueAsString(
                    mapper.createObjectNode().put("orderId", orderId)
                );
                kafkaTemplate.send("inventory-reserved", orderId, response);
                System.out.println("✅ Estoque reservado para pedido " + orderId);

            } else {
                String response = mapper.writeValueAsString(
                    mapper.createObjectNode()
                        .put("orderId", orderId)
                        .put("reason", "Estoque insuficiente")
                );
                kafkaTemplate.send("inventory-reservation-failed", orderId, response);
                System.out.println("❌ Estoque insuficiente para pedido " + orderId);
            }
        } catch (Exception e) {
            System.err.println("Erro ao processar pedido: " + e.getMessage());
        }
    }

    @KafkaListener(topics = "compensate-inventory", groupId = "inventory-group")
    public void handleCompensation(String message) {
        try {
            JsonNode node = mapper.readTree(message);
            String orderId = node.get("orderId").asText();

            Optional<InventoryItem> optional = repository.findById("abc");
            if (optional.isPresent()) {
                InventoryItem item = optional.get();
                item.setAvailableQuantity(item.getAvailableQuantity() + 1);
                repository.save(item);
                System.out.println("↩️ Estoque compensado para pedido " + orderId);
            }
        } catch (Exception e) {
            System.err.println("Erro na compensação: " + e.getMessage());
        }
    }
}
