using EventBus.MassTransit.Contracts;
using FluentValidation;
using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Transfer.API.Data;
using Transfer.Domain.Entities;

namespace Transfer.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TransfersController : ControllerBase
{
    private readonly TransferDbContext _context;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<TransfersController> _logger;
    private readonly IValidator<TransferRequest> _transferValidator;

    public TransfersController(
        TransferDbContext context,
        IPublishEndpoint publishEndpoint,
        ILogger<TransfersController> logger,
        IValidator<TransferRequest> transferValidator)
    {
        _context = context;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
        _transferValidator = transferValidator;
    }

    [HttpPost]
    public async Task<IActionResult> InitiateTransfer([FromBody] TransferRequest request)
    {
        // Manual FluentValidation for .NET 8 compatibility
        var validationResult = await _transferValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            return BadRequest(new { errors = validationResult.Errors.Select(e => new { field = e.PropertyName, message = e.ErrorMessage }) });
        }

        var userIdClaim = User.FindFirst("userId")?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new { error = "Invalid authentication token" });
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
        await _publishEndpoint.Publish<IDebitAccountRequested>(new
        {
            TransferId = transfer.Id,
            AccountId = transfer.FromAccountId,
            transfer.Amount
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
        var userIdClaim = User.FindFirst("userId")?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new { error = "Invalid authentication token" });
        }

        var transfer = await _context.Transfers.FindAsync(id);
        if (transfer == null)
        {
            return NotFound(new { error = "Transfer not found" });
        }

        // Authorization check: ensure user can only view their own transfers
        if (transfer.InitiatedBy != userId)
        {
            return NotFound(new { error = "Transfer not found" }); // Return NotFound for privacy
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
        // Authorization: Verify authenticated user matches requested userId
        var userIdClaim = User.FindFirst("userId")?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var authenticatedUserId))
        {
            return Unauthorized(new { error = "Invalid authentication token" });
        }

        // Users can only view their own transfers
        if (authenticatedUserId != userId)
        {
            return Forbid(); // Return 403 Forbidden
        }

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
