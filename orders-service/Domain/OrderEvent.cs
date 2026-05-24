namespace orders_service.Domain;

public class OrderEvent
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Data { get; set; } = string.Empty; // JSON
    public DateTime Timestamp { get; set; }
}