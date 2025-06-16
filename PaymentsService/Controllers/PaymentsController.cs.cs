using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PaymentsService.Data;
using PaymentsService.Models;

namespace PaymentsService.Controllers;
[ApiController]
[Route("payments")]
public class PaymentsController : ControllerBase
{
    private readonly PaymentsDbContext _db;
    public PaymentsController(PaymentsDbContext db) => _db = db;

    // Создать счёт
    [HttpPost("create")]  
    public async Task<IActionResult> CreateAccount(Guid userId)
    {
        if (await _db.Accounts.AnyAsync(a => a.UserId == userId))
            return BadRequest("Account exists");

        var account = new Account { Id = Guid.NewGuid(), UserId = userId, Balance = 0 };
        _db.Accounts.Add(account);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetBalance), new { userId }, account);
    }

    // Пополнение
    [HttpPost("topup")]
    public async Task<IActionResult> TopUp(Guid userId, decimal amount)
    {
        var account = await _db.Accounts.SingleOrDefaultAsync(a => a.UserId == userId);
        if (account is null) return NotFound();

        account.Balance += amount;
        await _db.SaveChangesAsync();
        return Ok(account);
    }

    // Баланс
    [HttpGet("balance")]
    public async Task<IActionResult> GetBalance(Guid userId)
    {
        var account = await _db.Accounts.SingleOrDefaultAsync(a => a.UserId == userId);
        return account is null ? NotFound() : Ok(new { account.Balance });
    }
}