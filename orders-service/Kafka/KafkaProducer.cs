using Confluent.Kafka;

namespace orders_service.Kafka;

public class KafkaProducer
{
    private readonly ProducerConfig _config;

    public KafkaProducer(IConfiguration configuration)
    {
        _config = new ProducerConfig
        {
            BootstrapServers = configuration["Kafka:BootstrapServers"] ?? "localhost:9092"
        };
    }

    public async Task ProduceAsync(string topic, string key, string value)
    {
        using var producer = new ProducerBuilder<string, string>(_config).Build();
        var message = new Message<string, string>
        {
            Key = key,
            Value = value
        };
        await producer.ProduceAsync(topic, message);
    }
}
