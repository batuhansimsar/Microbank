using Account.API.Data;
using Account.Domain.Entities;
using EventBus.MassTransit.Contracts;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace Account.API.EventHandlers;

/// <summary>
/// Handles debit account requests from Transfer Service
/// </summary>
public class DebitAccountRequestedConsumer : IConsumer<IDebitAccountRequested>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<DebitAccountRequestedConsumer> _logger;

    public DebitAccountRequestedConsumer(
        IServiceProvider serviceProvider,
        IPublishEndpoint publishEndpoint,
        ILogger<DebitAccountRequestedConsumer> logger)
    {
        _serviceProvider = serviceProvider;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<IDebitAccountRequested> context)
    {
        var message = context.Message;
        
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AccountDbContext>();

        var account = await dbContext.Accounts.FindAsync(message.AccountId);
        
        if (account == null)
        {
            _logger.LogWarning("Account not found: {AccountId}", message.AccountId);
            await _publishEndpoint.Publish<IAccountOperationFailed>(new
            {
                message.TransferId,
                message.AccountId,
                Reason = "Account not found",
                OperationType = "Debit"
            });
            return;
        }

        if (account.Balance < message.Amount)
        {
            _logger.LogWarning("Insufficient balance for account: {AccountId}", message.AccountId);
            await _publishEndpoint.Publish<IAccountOperationFailed>(new
            {
                message.TransferId,
                message.AccountId,
                Reason = "Insufficient balance",
                OperationType = "Debit"
            });
            return;
        }

        // Debit the account
        account.Balance -= message.Amount;
        
        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            AccountId = account.Id,
            Type = TransactionType.Debit,
            Amount = message.Amount,
            TransferId = message.TransferId,
            Description = $"Transfer debit - Transfer ID: {message.TransferId}",
            Timestamp = DateTime.UtcNow
        };

        dbContext.Transactions.Add(transaction);
        await dbContext.SaveChangesAsync();

        _logger.LogInformation("Account debited: {AccountId}, Amount: {Amount}", account.Id, message.Amount);

        await _publishEndpoint.Publish<IAccountDebited>(new
        {
            message.TransferId,
            message.AccountId,
            message.Amount
        });
    }
}

/// <summary>
/// Handles credit account requests from Transfer Service
/// </summary>
public class CreditAccountRequestedConsumer : IConsumer<ICreditAccountRequested>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<CreditAccountRequestedConsumer> _logger;

    public CreditAccountRequestedConsumer(
        IServiceProvider serviceProvider,
        IPublishEndpoint publishEndpoint,
        ILogger<CreditAccountRequestedConsumer> logger)
    {
        _serviceProvider = serviceProvider;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ICreditAccountRequested> context)
    {
        var message = context.Message;
        
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AccountDbContext>();

        // 3️⃣ IDEMPOTENCY: Check if this transfer already credited
        var existingCredit = await dbContext.Transactions
            .FirstOrDefaultAsync(t => 
                t.TransferId == message.TransferId && 
                t.Type == TransactionType.Credit &&
                t.AccountId == message.AccountId);
                
        if (existingCredit != null)
        {
            _logger.LogInformation("Transfer {TransferId} already credited to account {AccountId}, skipping", 
                message.TransferId, message.AccountId);
            return;
        }

        var account = await dbContext.Accounts.FindAsync(message.AccountId);
        
        if (account == null)
        {
            _logger.LogWarning("Account not found: {AccountId}", message.AccountId);
            await _publishEndpoint.Publish<IAccountOperationFailed>(new
            {
                message.TransferId,
                message.AccountId,
                Reason = "Account not found",
                OperationType = "Credit"
            });
            return;
        }

        // Credit the account
        account.Balance += message.Amount;
        
        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            AccountId = account.Id,
            Type = TransactionType.Credit,
            Amount = message.Amount,
            TransferId = message.TransferId,
            Description = $"Transfer credit - Transfer ID: {message.TransferId}",
            Timestamp = DateTime.UtcNow
        };

        dbContext.Transactions.Add(transaction);
        
        try
        {
            await dbContext.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            // 1️⃣ OPTIMISTIC CONCURRENCY: Let MassTransit retry handle this
            _logger.LogWarning("Concurrency conflict while crediting account {AccountId}, will retry", message.AccountId);
            throw; // Rethrow for MassTransit retry
        }

        _logger.LogInformation("Account credited: {AccountId}, Amount: {Amount}", account.Id, message.Amount);

        await _publishEndpoint.Publish<IAccountCredited>(new
        {
            message.TransferId,
            message.AccountId,
            message.Amount
        });
    }
}

/// <summary>
/// Handles compensation events to reverse failed transfers
/// </summary>
public class CompensateDebitConsumer : IConsumer<ICompensateDebit>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CompensateDebitConsumer> _logger;

    public CompensateDebitConsumer(
        IServiceProvider serviceProvider,
        ILogger<CompensateDebitConsumer> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ICompensateDebit> context)
    {
        var message = context.Message;
        
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AccountDbContext>();

        var account = await dbContext.Accounts.FindAsync(message.AccountId);
        
        if (account != null)
        {
            // Reverse the debit (credit it back)
            account.Balance += message.Amount;
            
            var transaction = new Transaction
            {
                Id = Guid.NewGuid(),
                AccountId = account.Id,
                Type = TransactionType.Credit,
                Amount = message.Amount,
                TransferId = message.TransferId,
                Description = $"Compensation for failed transfer - Transfer ID: {message.TransferId}",
                Timestamp = DateTime.UtcNow
            };

            dbContext.Transactions.Add(transaction);
            await dbContext.SaveChangesAsync();

            _logger.LogInformation("Debit compensated for account: {AccountId}, Amount: {Amount}", 
                account.Id, message.Amount);
        }
    }
}
