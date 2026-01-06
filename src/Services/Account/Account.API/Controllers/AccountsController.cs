using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Account.API.Data;
using Account.Domain.Entities;

namespace Account.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AccountsController : ControllerBase
{
    private readonly AccountDbContext _context;
    private readonly ILogger<AccountsController> _logger;

    public AccountsController(AccountDbContext context, ILogger<AccountsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> CreateAccount()
    {
        var userIdClaim = User.FindFirst("userId")?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new { error = "Invalid authentication token" });
        }

        // Generate unique account number
        var accountNumber = GenerateAccountNumber();

        var account = new BankAccount
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            AccountNumber = accountNumber,
            Balance = 1000.00m, // Initial balance for testing
            Currency = "TRY",
            CreatedAt = DateTime.UtcNow
        };

        _context.Accounts.Add(account);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Account created: {AccountNumber} for user {UserId}", accountNumber, userId);

        return Ok(new
        {
            account.Id,
            account.AccountNumber,
            account.Balance,
            account.Currency,
            account.CreatedAt
        });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetAccount(Guid id)
    {
        var account = await _context.Accounts
            .Include(a => a.Transactions)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (account == null)
        {
            return NotFound(new { error = "Account not found" });
        }

        return Ok(new
        {
            account.Id,
            account.AccountNumber,
            account.Balance,
            account.Currency,
            account.CreatedAt
        });
    }

    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetUserAccounts(Guid userId)
    {
        // Authorization: Verify authenticated user matches requested userId
        var userIdClaim = User.FindFirst("userId")?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var authenticatedUserId))
        {
            return Unauthorized(new { error = "Invalid authentication token" });
        }

        // Users can only view their own accounts
        if (authenticatedUserId != userId)
        {
            return Forbid(); // Return 403 Forbidden
        }

        var accounts = await _context.Accounts
            .Where(a => a.UserId == userId)
            .ToListAsync();

        return Ok(accounts.Select(a => new
        {
            a.Id,
            a.AccountNumber,
            a.Balance,
            a.Currency,
            a.CreatedAt
        }));
    }

    [HttpGet("{id}/balance")]
    public async Task<IActionResult> GetBalance(Guid id)
    {
        var account = await _context.Accounts.FindAsync(id);
        
        if (account == null)
        {
            return NotFound(new { error = "Account not found" });
        }

        return Ok(new { account.Balance, account.Currency });
    }

    [HttpGet("{id}/transactions")]
    public async Task<IActionResult> GetTransactions(Guid id)
    {
        var transactions = await _context.Transactions
            .Where(t => t.AccountId == id)
            .OrderByDescending(t => t.Timestamp)
            .ToListAsync();

        return Ok(transactions);
    }

    private string GenerateAccountNumber()
    {
        // Thread-safe random generation with timestamp for uniqueness
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var randomPart1 = Random.Shared.Next(100000, 999999);
        var randomPart2 = Random.Shared.Next(100000, 999999);
        return $"TR{timestamp % 100:D2}{randomPart1:D6}{randomPart2:D6}";
    }
}
