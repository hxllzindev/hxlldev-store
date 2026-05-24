#!/bin/bash
echo "🚀 Iniciando HxllDevStore Saga Pattern..."

# Docker
cd ~/Desktop/hxlldev-store
docker-compose up -d
sleep 30

# Tópicos Kafka (se necessário)
docker exec -it kafka kafka-topics --bootstrap-server localhost:9092 --create --topic order-created --partitions 1 --replication-factor 1 2>/dev/null
# ... (repita para todos os tópicos, como no passo 2)

# Orders Service (abre terminal separado no macOS)
osascript -e 'tell app "Terminal" to do script "cd ~/Desktop/hxlldev-store/orders-service && dotnet run"'
sleep 5

# Inventory Service
osascript -e 'tell app "Terminal" to do script "cd ~/Desktop/hxlldev-store/inventory-service && mvn spring-boot:run"'
sleep 5

# Payments Service
osascript -e 'tell app "Terminal" to do script "cd ~/Desktop/hxlldev-store/payments-service && source venv/bin/activate && python -m uvicorn main:app --port 8002 --reload"'

echo "✅ Todos os serviços iniciados!"