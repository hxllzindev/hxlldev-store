using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using orders_service.Data;
using orders_service.Domain;
using orders_service.Kafka;

var builder = WebApplication.CreateBuilder(args);


builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNameCaseInsensitive = true;
});

// Configurar PostgreSQL
builder.Services.AddDbContext<OrderDbContext>(options =>
    options.UseNpgsql("Host=localhost;Database=orders_db;Username=saga;Password=saga123"));

// Registrar serviços Kafka
builder.Services.AddSingleton<KafkaProducer>();
builder.Services.AddHostedService<SagaOrchestrator>();

var app = builder.Build();

// Endpoint para criar pedido
app.MapPost("/orders", async (CreateOrderRequest request, OrderDbContext db, KafkaProducer kafka) =>
{
    try
    {
        // Aplicar valor padrão para CustomerId se vazio (começo: "001")
        var customerId = string.IsNullOrWhiteSpace(request.CustomerId) ? "001" : request.CustomerId;
        var requestWithDefault = request with { CustomerId = customerId };

        if (requestWithDefault.Items == null || requestWithDefault.Items.Count == 0)
            return Results.BadRequest(new { error = "Items não pode estar vazio" });

        var orderId = Guid.NewGuid();

        // Salvar evento no banco (Event Sourcing)
        var evt = new OrderEvent
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            EventType = "PedidoCriado",
            Data = JsonSerializer.Serialize(requestWithDefault),
            Timestamp = DateTime.UtcNow
        };

        db.Events.Add(evt);
        await db.SaveChangesAsync();

        // Publicar no Kafka (sem esperar, para não travar)
        try
        {
            var message = JsonSerializer.Serialize(new
            {
                orderId = orderId.ToString(),
                customerId = requestWithDefault.CustomerId,
                items = requestWithDefault.Items
            });

            _ = kafka.ProduceAsync("order-created", orderId.ToString(), message).ConfigureAwait(false);
        }
        catch (Exception kafkaEx)
        {
            Console.WriteLine($"⚠️ Erro ao publicar no Kafka: {kafkaEx.Message}");
            // Não falha a requisição se Kafka falhar - o evento foi salvo no banco
        }

        return Results.Created($"/orders/{orderId}", new
        {
            orderId,
            status = "pendente",
            message = "Pedido criado e enviado para processamento"
        });
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Erro ao criar pedido: {ex.Message}");
        return Results.Problem(detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError, title: "Erro ao criar pedido");
    }
});

// Endpoint para consultar status do pedido
app.MapGet("/orders/{orderId}", async (string orderId, OrderDbContext db) =>
{
    try
    {
        // Validar formato GUID
        if (!Guid.TryParse(orderId, out var parsedOrderId))
            return Results.BadRequest(new { error = "orderId deve ser um GUID válido" });

        var events = await db.Events
            .Where(e => e.OrderId == parsedOrderId)
            .OrderBy(e => e.Timestamp)
            .ToListAsync();

        if (events.Count == 0)
            return Results.NotFound(new { error = $"Nenhum pedido encontrado com ID: {orderId}" });

        return Results.Ok(events.Select(e => new
        {
            e.EventType,
            e.Data,
            e.Timestamp
        }));
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Erro ao consultar pedido: {ex.Message}");
        return Results.Problem(detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError, title: "Erro ao consultar pedido");
    }
});

app.UseDefaultFiles();
app.UseStaticFiles();

app.Run();

// Models
record CreateOrderRequest(string CustomerId, List<OrderItem> Items);
record OrderItem(string ProductId, int Quantity);