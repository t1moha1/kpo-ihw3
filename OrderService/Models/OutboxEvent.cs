namespace OrdersService.Models;
public class OutboxEvent
{
    public Guid Id { get; set; }
    public string Topic { get; set; } = null!;
    public string Payload { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? PublishedAt { get; set; }
}