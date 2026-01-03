using EventBus.RabbitMQ;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Transfer.API.Data;
using Transfer.API.Events;
using Transfer.Domain.Entities;

namespace Transfer.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TransfersController : ControllerBase
{
    private readonly TransferDbContext _context;
    private readonly IEventBus _eventBus;
    private readonly ILogger<TransfersController> _logger;

    public TransfersController(
        TransferDbContext context,
        IEventBus eventBus,
        ILogger<TransfersController> logger)
    {
        _context = context;
        _eventBus = eventBus;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> InitiateTransfer([FromBody] TransferRequest request)
    {
        var userIdClaim = User.FindFirst("userId")?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new { error = "Invalid authentication token" });
        }

        if (request.FromAccountId == request.ToAccountId)
        {
            return BadRequest(new { error = "Cannot transfer to the same account" });
        }

        if (request.Amount <= 0)
        {
            return BadRequest(new { error = "Amount must be greater than zero" });
        }

        // Create transfer record (SAGA Step 1)
        var transfer = new MoneyTransfer
        {
            Id = Guid.NewGuid(),
            FromAccountId = request.FromAccountId,
            ToAccountId = request.ToAccountId,
            Amount = request.Amount,
            Currency = request.Currency ?? "TRY",
            Status = TransferStatus.Pending,
            InitiatedBy = userId,
            InitiatedAt = DateTime.UtcNow
        };

        _context.Transfers.Add(transfer);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Transfer initiated: {TransferId}, Amount: {Amount} {Currency}", 
            transfer.Id, transfer.Amount, transfer.Currency);

        // Start SAGA: Request debit
        await _eventBus.PublishAsync(new DebitAccountRequestedEvent
        {
            TransferId = transfer.Id,
            AccountId = transfer.FromAccountId,
            Amount = transfer.Amount
        });

        return Ok(new
        {
            transferId = transfer.Id,
            status = transfer.Status.ToString(),
            amount = transfer.Amount,
            currency = transfer.Currency,
            initiatedAt = transfer.InitiatedAt
        });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetTransfer(Guid id)
    {
        var transfer = await _context.Transfers.FindAsync(id);
        if (transfer == null)
        {
            return NotFound(new { error = "Transfer not found" });
        }

        return Ok(new
        {
            transfer.Id,
            transfer.FromAccountId,
            transfer.ToAccountId,
            transfer.Amount,
            transfer.Currency,
            Status = transfer.Status.ToString(),
            transfer.InitiatedAt,
            transfer.CompletedAt,
            transfer.FailureReason
        });
    }

    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetUserTransfers(Guid userId)
    {
        var transfers = await _context.Transfers
            .Where(t => t.InitiatedBy == userId)
            .OrderByDescending(t => t.InitiatedAt)
            .ToListAsync();

        return Ok(transfers.Select(t => new
        {
            t.Id,
            t.FromAccountId,
            t.ToAccountId,
            t.Amount,
            t.Currency,
            Status = t.Status.ToString(),
            t.InitiatedAt,
            t.CompletedAt,
            t.FailureReason
        }));
    }
}

public record TransferRequest(
    Guid FromAccountId,
    Guid ToAccountId,
    decimal Amount,
    string? Currency
);
