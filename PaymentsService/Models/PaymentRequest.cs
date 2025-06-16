namespace PaymentsService.Models;
public record PaymentRequest(Guid OrderId, Guid UserId, decimal Amount);