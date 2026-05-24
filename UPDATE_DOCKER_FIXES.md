# Docker Runtime Fixes

Este update corrige a inicializacao do projeto via Docker Compose, garantindo que os microservicos sejam iniciados apenas com:

```bash
docker-compose up -d --build
```

Antes, alguns servicos eram buildados, mas nao ficavam acessiveis corretamente pelo navegador ou encerravam apos iniciar.

## Problemas encontrados

- O `orders-service` mirava `net10.0`, mas o Dockerfile usava imagens `.NET 8`.
- O `orders-service` nao estava configurado explicitamente para escutar na porta `5106` dentro do container.
- Orders e Payments tentavam acessar Kafka/Postgres usando `localhost`.
- Dentro de containers Docker, `localhost` aponta para o proprio container, nao para outros servicos.
- Kafka e ZooKeeper nao estavam na mesma rede Docker customizada dos demais servicos.
- O `payments-service` executava `python3 main.py`, importava a aplicacao FastAPI e encerrava, em vez de manter um servidor HTTP ativo.

## Correcoes aplicadas

- Atualizadas as imagens do Orders para `.NET 10`:
  - `mcr.microsoft.com/dotnet/sdk:10.0`
  - `mcr.microsoft.com/dotnet/aspnet:10.0`
- Adicionada a variavel `ASPNETCORE_URLS=http://+:5106` no `orders-service`.
- Adicionada a configuracao `Kafka__BootstrapServers=kafka:9092` no `orders-service`.
- O Orders agora le a connection string via `ConnectionStrings__DefaultConnection`.
- O `KafkaProducer` e o `SagaOrchestrator` agora leem o bootstrap server do Kafka via configuracao.
- O `payments-service` agora le:
  - `DATABASE_URL`
  - `KAFKA_BOOTSTRAP_SERVERS`
- O Dockerfile do Payments agora inicia a API com Uvicorn:

```bash
uvicorn main:app --host 0.0.0.0 --port 8002
```

- Kafka e ZooKeeper foram adicionados a rede `hxll-network`.
- Kafka agora anuncia `PLAINTEXT://kafka:9092` para comunicacao interna entre containers.

## Como iniciar agora

Na raiz do projeto:

```bash
docker-compose up -d --build
```

Para verificar o status:

```bash
docker-compose ps
```

Resultado esperado:

```text
orders-service      Up   0.0.0.0:5106->5106/tcp
payments-service    Up   0.0.0.0:8002->8002/tcp
inventory-service   Up   0.0.0.0:8080->8080/tcp
kafka               Up   0.0.0.0:9092->9092/tcp
postgres            Up   0.0.0.0:5432->5432/tcp
zookeeper           Up
```

## URLs

- Orders dashboard: http://localhost:5106
- Inventory service: http://localhost:8080
- Payments service: http://localhost:8002
- Kafka: `localhost:9092`
- PostgreSQL: `localhost:5432`

Se `localhost` nao responder no navegador, testar:

```text
http://127.0.0.1:5106
```

## Observacao importante

Nao e necessario abrir terminais separados para Orders, Inventory ou Payments. O Docker Compose ja sobe todos os servicos.

O script `start-all.sh` mistura containers Docker com execucao local dos servicos e nao deve ser o fluxo principal para este setup.

