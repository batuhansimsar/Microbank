using Account.API.Data;
using Account.API.Events;
using Account.Domain.Entities;
using EventBus.RabbitMQ;
using Microsoft.EntityFrameworkCore;

namespace Account.API.EventHandlers;

public class DebitAccountRequestedEventHandler : IIntegrationEventHandler<DebitAccountRequestedEvent>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IEventBus _eventBus;
    private readonly ILogger<DebitAccountRequestedEventHandler> _logger;

    public DebitAccountRequestedEventHandler(
        IServiceProvider serviceProvider,
        IEventBus eventBus,
        ILogger<DebitAccountRequestedEventHandler> logger)
    {
        _serviceProvider = serviceProvider;
        _eventBus = eventBus;
        _logger = logger;
    }

    public async Task HandleAsync(DebitAccountRequestedEvent @event)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AccountDbContext>();

        var account = await context.Accounts.FindAsync(@event.AccountId);
        
        if (account == null)
        {
            _logger.LogWarning("Account not found: {AccountId}", @event.AccountId);
            await _eventBus.PublishAsync(new AccountOperationFailedEvent
            {
                TransferId = @event.TransferId,
                AccountId = @event.AccountId,
                Reason = "Account not found",
                OperationType = "Debit"
            });
            return;
        }

        if (account.Balance < @event.Amount)
        {
            _logger.LogWarning("Insufficient balance for account: {AccountId}", @event.AccountId);
            await _eventBus.PublishAsync(new AccountOperationFailedEvent
            {
                TransferId = @event.TransferId,
                AccountId = @event.AccountId,
                Reason = "Insufficient balance",
                OperationType = "Debit"
            });
            return;
        }

        // Debit the account
        account.Balance -= @event.Amount;
        
        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            AccountId = account.Id,
            Type = TransactionType.Debit,
            Amount = @event.Amount,
            TransferId = @event.TransferId,
            Description = $"Transfer debit - Transfer ID: {@event.TransferId}",
            Timestamp = DateTime.UtcNow
        };

        context.Transactions.Add(transaction);
        await context.SaveChangesAsync();

        _logger.LogInformation("Account debited: {AccountId}, Amount: {Amount}", account.Id, @event.Amount);

        await _eventBus.PublishAsync(new AccountDebitedEvent
        {
            TransferId = @event.TransferId,
            AccountId = @event.AccountId,
            Amount = @event.Amount
        });
    }
}

public class CreditAccountRequestedEventHandler : IIntegrationEventHandler<CreditAccountRequestedEvent>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IEventBus _eventBus;
    private readonly ILogger<CreditAccountRequestedEventHandler> _logger;

    public CreditAccountRequestedEventHandler(
        IServiceProvider serviceProvider,
        IEventBus eventBus,
        ILogger<CreditAccountRequestedEventHandler> logger)
    {
        _serviceProvider = serviceProvider;
        _eventBus = eventBus;
        _logger = logger;
    }

    public async Task HandleAsync(CreditAccountRequestedEvent @event)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AccountDbContext>();

        var account = await context.Accounts.FindAsync(@event.AccountId);
        
        if (account == null)
        {
            _logger.LogWarning("Account not found: {AccountId}", @event.AccountId);
            await _eventBus.PublishAsync(new AccountOperationFailedEvent
            {
                TransferId = @event.TransferId,
                AccountId = @event.AccountId,
                Reason = "Account not found",
                OperationType = "Credit"
            });
            return;
        }

        // Credit the account
        account.Balance += @event.Amount;
        
        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            AccountId = account.Id,
            Type = TransactionType.Credit,
            Amount = @event.Amount,
            TransferId = @event.TransferId,
            Description = $"Transfer credit - Transfer ID: {@event.TransferId}",
            Timestamp = DateTime.UtcNow
        };

        context.Transactions.Add(transaction);
        await context.SaveChangesAsync();

        _logger.LogInformation("Account credited: {AccountId}, Amount: {Amount}", account.Id, @event.Amount);

        await _eventBus.PublishAsync(new AccountCreditedEvent
        {
            TransferId = @event.TransferId,
            AccountId = @event.AccountId,
            Amount = @event.Amount
        });
    }
}

public class CompensateDebitEventHandler : IIntegrationEventHandler<CompensateDebitEvent>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CompensateDebitEventHandler> _logger;

    public CompensateDebitEventHandler(
        IServiceProvider serviceProvider,
        ILogger<CompensateDebitEventHandler> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task HandleAsync(CompensateDebitEvent @event)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AccountDbContext>();

        var account = await context.Accounts.FindAsync(@event.AccountId);
        
        if (account != null)
        {
            // Reverse the debit (credit it back)
            account.Balance += @event.Amount;
            
            var transaction = new Transaction
            {
                Id = Guid.NewGuid(),
                AccountId = account.Id,
                Type = TransactionType.Credit,
                Amount = @event.Amount,
                TransferId = @event.TransferId,
                Description = $"Compensation for failed transfer - Transfer ID: {@event.TransferId}",
                Timestamp = DateTime.UtcNow
            };

            context.Transactions.Add(transaction);
            await context.SaveChangesAsync();

            _logger.LogInformation("Debit compensated for account: {AccountId}, Amount: {Amount}", 
                account.Id, @event.Amount);
        }
    }
}
