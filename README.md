# HXLLDEV Store

Visão geral
- Projeto composto por microserviços: Orders (ASP.NET Minimal API), Inventory (Spring Boot), Payments (Python).
- Comunicação entre serviços por eventos usando Kafka; eventos persistidos em Postgres (event store).
- Padrões principais: Saga para orquestração de transações distribuídas e Event Sourcing para histórico de eventos.

Fluxo resumido (passo a passo)
1. O cliente cria um pedido via frontend (`POST /orders`). Se `customerId` não for enviado, o serviço aplica `001` como padrão.
2. O Orders service valida, persiste um `OrderEvent` no banco e publica um evento (`order-created`) no Kafka.
3. Inventory e Payments consomem o evento e executam suas ações (reservar estoque, processar pagamento).
4. O `SagaOrchestrator` coordena o fluxo, lidando com confirmações e compensações quando necessário.

Como resumir rápido

- Tecnologias: C# (.NET / ASP.NET Minimal API), Java (Spring Boot), Python, Kafka, PostgreSQL, Docker, HTML/CSS/JS (frontend).

- Funcionalidades principais:
	- Criação de pedidos (`POST /orders`) com valor padrão de `customerId` = "001" quando não informado.
	- Event Sourcing: eventos de pedido persistidos em Postgres.
	- Orquestração via Saga: coordenação de reserva de estoque e processamento de pagamento.
	- Dashboard simples para criar pedidos e visualizar eventos de Orders e Payments.

Como executar (um comando):

```bash
docker-compose up
```

Pronto! Todos os serviços, banco de dados e message broker levantam de uma vez:
- **Orders**: http://localhost:5106 (dashboard com frontend incluído)
- **Inventory**: http://localhost:8080
- **Payments**: http://localhost:8002
- **Kafka**: localhost:9092
- **PostgreSQL**: localhost:5432

O projeto é intencionalmente compacto para demonstrar arquitetura distribuída, padrões de consistência e experiência prática com mensageria e persistência de eventos.
