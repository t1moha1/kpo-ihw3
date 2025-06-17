using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrdersService.Data;
using OrdersService.Models;
using System.Text.Json;

namespace OrdersService.Controllers;

[ApiController]
[Route("orders")]
public class OrdersController : ControllerBase
{
    private readonly OrdersDbContext _db;
    public OrdersController(OrdersDbContext db) => _db = db;

    [HttpPost]
    public async Task<IActionResult> Create(Guid userId, decimal amount, string description)
    {
        var order = new Order { Id = Guid.NewGuid(), UserId = userId, Amount = amount, Description = description };
        var evt = new OutboxEvent {
            Id = Guid.NewGuid(),
            Topic = "orders.created",
            Payload = JsonSerializer.Serialize(new { OrderId = order.Id, UserId = order.UserId, Amount = order.Amount })
        };

        await using var tx = await _db.Database.BeginTransactionAsync();
        _db.Orders.Add(order);
        _db.OutboxEvents.Add(evt);
        await _db.SaveChangesAsync();
        await tx.CommitAsync();

        return Accepted(order);
    }

    [HttpGet]
    public async Task<IActionResult> List(Guid userId)
    {
        var list = await _db.Orders.Where(o => o.UserId == userId).ToListAsync();
        return Ok(list);
    }

    [HttpGet("{orderId}")]
    public async Task<IActionResult> Get(Guid orderId)
    {
        var order = await _db.Orders.FindAsync(orderId);
        return order is null ? NotFound() : Ok(order);
    }
}