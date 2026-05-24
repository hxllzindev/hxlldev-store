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

- Tecnologias: C# (.NET / ASP.NET Minimal API), Java (Spring Boot), Python, Kafka, PostgreSQL, Docker, HTML/CSS/JS (frontend).

- Funcionalidades principais:
	- Criação de pedidos (`POST /orders`) com valor padrão de `customerId` = "001" quando não informado.
	- Event Sourcing: eventos de pedido persistidos em Postgres.
	- Orquestração via Saga: coordenação de reserva de estoque e processamento de pagamento.
	- Dashboard simples para criar pedidos e visualizar eventos de Orders e Payments.

Estrutura da aplicação

```text
hxlldev-store/
├── docker-compose.yml        # Orquestra todos os serviços, Kafka, ZooKeeper e PostgreSQL
├── init-databases.sql        # Cria os bancos usados pelos microserviços
├── orders-service/           # Serviço de pedidos em ASP.NET Minimal API
│   ├── Program.cs            # Endpoints HTTP, frontend estático e configuração principal
│   ├── Data/                 # DbContext do Event Store de pedidos
│   ├── Domain/               # Entidades de domínio do Orders
│   ├── Kafka/                # Producer Kafka e SagaOrchestrator
│   └── wwwroot/              # Dashboard web servido pelo Orders
├── inventory-service/        # Serviço de estoque em Spring Boot
│   └── src/                  # Código Java, consumidores Kafka e regras de estoque
├── payments-service/         # Serviço de pagamentos em Python/FastAPI
│   ├── main.py               # API HTTP, consumer Kafka e processamento de pagamentos
│   └── database.py           # Configuração SQLAlchemy e eventos de pagamento
└── UPDATE_DOCKER_FIXES.md    # Registro das correções aplicadas no runtime Docker
```

Responsabilidades principais:
- **Orders Service**: recebe pedidos, persiste eventos, publica `order-created` e orquestra a Saga.
- **Inventory Service**: consome eventos de pedido e reserva ou rejeita estoque.
- **Payments Service**: consome comandos de pagamento, registra eventos e publica aprovação ou rejeição.
- **Kafka**: transporta os eventos entre os microserviços.
- **PostgreSQL**: armazena os bancos separados de Orders, Inventory e Payments.

Como executar

```bash
docker-compose up -d --build
```

Pronto! Todos os serviços, banco de dados e message broker sobem em segundo plano via Docker Compose. Não é necessário abrir terminais separados para Orders, Inventory ou Payments.

Para verificar se tudo está rodando:

```bash
docker-compose ps
```

Resultado esperado:

```text
orders-service      Up   0.0.0.0:5106->5106/tcp
inventory-service   Up   0.0.0.0:8080->8080/tcp
payments-service    Up   0.0.0.0:8002->8002/tcp
kafka               Up   0.0.0.0:9092->9092/tcp
postgres            Up   0.0.0.0:5432->5432/tcp
zookeeper           Up
```

Acessos:
- **Orders**: http://localhost:5106 (dashboard com frontend incluído)
- **Inventory**: http://localhost:8080
- **Payments**: http://localhost:8002
- **Kafka**: localhost:9092
- **PostgreSQL**: localhost:5432

Se `localhost` não responder no navegador, tente:

```text
http://127.0.0.1:5106
```

Para parar tudo:

```bash
docker-compose down
```

O projeto é intencionalmente compacto para demonstrar arquitetura distribuída, padrões de consistência e experiência prática com mensageria e persistência de eventos.
