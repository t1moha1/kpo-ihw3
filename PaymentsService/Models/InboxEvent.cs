namespace PaymentsService.Models;
public class InboxEvent
{
    public Guid Id { get; set; }
    public string Topic { get; set; } = null!;
    public string Payload { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool Processed { get; set; }
}