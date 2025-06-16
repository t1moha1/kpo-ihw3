namespace OrdersService.Models;
public enum OrderStatus { NEW, FINISHED, CANCELLED }

public class Order
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public decimal Amount { get; set; }
    public string Description { get; set; } = null!;
    public OrderStatus Status { get; set; } = OrderStatus.NEW;
}