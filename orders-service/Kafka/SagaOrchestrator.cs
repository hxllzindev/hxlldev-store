using System.Text.Json;
using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using orders_service.Data;
using orders_service.Domain;

namespace orders_service.Kafka;

public class SagaOrchestrator : BackgroundService
{
    private readonly IConsumer<string, string> _consumer;
    private readonly KafkaProducer _producer;
    private readonly IServiceScopeFactory _scopeFactory;

    public SagaOrchestrator(KafkaProducer producer, IServiceScopeFactory scopeFactory)
    {
        _producer = producer;
        _scopeFactory = scopeFactory;

        var config = new ConsumerConfig
        {
            BootstrapServers = "localhost:9092",
            GroupId = "orders-saga-group",
            AutoOffsetReset = AutoOffsetReset.Earliest
        };
        _consumer = new ConsumerBuilder<string, string>(config).Build();
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _consumer.Subscribe(new[]
        {
            "inventory-reserved",
            "inventory-reservation-failed",
            "payment-approved",
            "payment-rejected"
        });

        return Task.Run(async () =>
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var result = _consumer.Consume(stoppingToken);
                    var topic = result.Topic;
                    var messageValue = result.Message.Value;

                    using var scope = _scopeFactory.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<OrderDbContext>();

                    var jsonDoc = JsonDocument.Parse(messageValue);
                    var root = jsonDoc.RootElement;
                    var orderId = root.GetProperty("orderId").GetString()!;

                    switch (topic)
                    {
                        case "inventory-reserved":
                            // Próximo passo: solicitar pagamento
                            var paymentCommand = JsonSerializer.Serialize(new
                            {
                                orderId,
                                amount = 100.00 // valor mockado
                            });
                            await _producer.ProduceAsync("payment-commands", orderId, paymentCommand);
                            break;

                        case "inventory-reservation-failed":
                            // Estoque insuficiente — encerrar com falha
                            var cancelEvent = new OrderEvent
                            {
                                Id = Guid.NewGuid(),
                                OrderId = Guid.Parse(orderId),
                                EventType = "PedidoCancelado",
                                Data = "{\"reason\": \"Estoque insuficiente\"}",
                                Timestamp = DateTime.UtcNow
                            };
                            db.Events.Add(cancelEvent);
                            await db.SaveChangesAsync();
                            break;

                        case "payment-approved":
                            // Sucesso! Confirmar pedido
                            var confirmedEvent = new OrderEvent
                            {
                                Id = Guid.NewGuid(),
                                OrderId = Guid.Parse(orderId),
                                EventType = "PedidoConfirmado",
                                Data = "{}",
                                Timestamp = DateTime.UtcNow
                            };
                            db.Events.Add(confirmedEvent);
                            await db.SaveChangesAsync();

                            await _producer.ProduceAsync("order-confirmed", orderId, "{}");
                            break;

                        case "payment-rejected":
                            // Pagamento falhou — compensar estoque
                            var compensateCommand = JsonSerializer.Serialize(new { orderId });
                            await _producer.ProduceAsync("compensate-inventory", orderId, compensateCommand);

                            var failedEvent = new OrderEvent
                            {
                                Id = Guid.NewGuid(),
                                OrderId = Guid.Parse(orderId),
                                EventType = "PedidoCancelado",
                                Data = "{\"reason\": \"Pagamento recusado\"}",
                                Timestamp = DateTime.UtcNow
                            };
                            db.Events.Add(failedEvent);
                            await db.SaveChangesAsync();
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erro no orquestrador: {ex.Message}");
                }
            }
        }, stoppingToken);
    }

    public override void Dispose()
    {
        _consumer.Close();
        _consumer.Dispose();
        base.Dispose();
    }
}