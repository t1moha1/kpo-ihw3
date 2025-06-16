namespace PaymentsService.Models;
public record PaymentResultEvent(Guid OrderId, bool Success);