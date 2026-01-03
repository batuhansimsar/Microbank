using EventBus.RabbitMQ;
using Microsoft.EntityFrameworkCore;
using Transfer.API.Data;
using Transfer.API.Events;
using Transfer.Domain.Entities;

namespace Transfer.API.EventHandlers;

/// <summary>
/// SAGA Step 2: Account debited successfully, now credit the receiver
/// </summary>
public class AccountDebitedEventHandler : IIntegrationEventHandler<AccountDebitedEvent>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IEventBus _eventBus;
    private readonly ILogger<AccountDebitedEventHandler> _logger;

    public AccountDebitedEventHandler(
        IServiceProvider serviceProvider,
        IEventBus eventBus,
        ILogger<AccountDebitedEventHandler> logger)
    {
        _serviceProvider = serviceProvider;
        _eventBus = eventBus;
        _logger = logger;
    }

    public async Task HandleAsync(AccountDebitedEvent @event)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransferDbContext>();

        var transfer = await context.Transfers.FindAsync(@event.TransferId);
        if (transfer == null)
        {
            _logger.LogWarning("Transfer not found: {TransferId}", @event.TransferId);
            return;
        }

        // Update SAGA state
        transfer.Status = TransferStatus.DebitSuccessful;
        await context.SaveChangesAsync();

        _logger.LogInformation("Transfer {TransferId}: Debit successful, requesting credit", transfer.Id);

        // Publish credit request
        await _eventBus.PublishAsync(new CreditAccountRequestedEvent
        {
            TransferId = transfer.Id,
            AccountId = transfer.ToAccountId,
            Amount = transfer.Amount
        });
    }
}

/// <summary>
/// SAGA Step 3: Account credited successfully, transfer complete!
/// </summary>
public class AccountCreditedEventHandler : IIntegrationEventHandler<AccountCreditedEvent>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IEventBus _eventBus;
    private readonly ILogger<AccountCreditedEventHandler> _logger;

    public AccountCreditedEventHandler(
        IServiceProvider serviceProvider,
        IEventBus eventBus,
        ILogger<AccountCreditedEventHandler> logger)
    {
        _serviceProvider = serviceProvider;
        _eventBus = eventBus;
        _logger = logger;
    }

    public async Task HandleAsync(AccountCreditedEvent @event)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransferDbContext>();

        var transfer = await context.Transfers.FindAsync(@event.TransferId);
        if (transfer == null)
        {
            _logger.LogWarning("Transfer not found: {TransferId}", @event.TransferId);
            return;
        }

        // SAGA COMPLETE!
        transfer.Status = TransferStatus.Completed;
        transfer.CompletedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();

        _logger.LogInformation("Transfer {TransferId}: COMPLETED! {Amount} {Currency} transferred", 
            transfer.Id, transfer.Amount, transfer.Currency);

        // Notify success
        await _eventBus.PublishAsync(new TransferCompletedEvent
        {
            TransferId = transfer.Id,
            FromAccountId = transfer.FromAccountId,
            ToAccountId = transfer.ToAccountId,
            Amount = transfer.Amount,
            Currency = transfer.Currency
        });
    }
}

/// <summary>
/// SAGA Compensation: Something failed, handle it
/// </summary>
public class AccountOperationFailedEventHandler : IIntegrationEventHandler<AccountOperationFailedEvent>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IEventBus _eventBus;
    private readonly ILogger<AccountOperationFailedEventHandler> _logger;

    public AccountOperationFailedEventHandler(
        IServiceProvider serviceProvider,
        IEventBus eventBus,
        ILogger<AccountOperationFailedEventHandler> logger)
    {
        _serviceProvider = serviceProvider;
        _eventBus = eventBus;
        _logger = logger;
    }

    public async Task HandleAsync(AccountOperationFailedEvent @event)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransferDbContext>();

        var transfer = await context.Transfers.FindAsync(@event.TransferId);
        if (transfer == null)
        {
            _logger.LogWarning("Transfer not found: {TransferId}", @event.TransferId);
            return;
        }

        _logger.LogWarning("Transfer {TransferId}: Operation failed - {Reason}", 
            transfer.Id, @event.Reason);

        // If credit failed but debit was successful, compensate!
        if (@event.OperationType == "Credit" && transfer.Status == TransferStatus.DebitSuccessful)
        {
            _logger.LogInformation("Transfer {TransferId}: Compensating debit", transfer.Id);
            
            await _eventBus.PublishAsync(new CompensateDebitEvent
            {
                TransferId = transfer.Id,
                AccountId = transfer.FromAccountId,
                Amount = transfer.Amount
            });
        }

        // Mark transfer as failed
        transfer.Status = TransferStatus.Failed;
        transfer.CompletedAt = DateTime.UtcNow;
        transfer.FailureReason = @event.Reason;
        await context.SaveChangesAsync();

        // Notify failure
        await _eventBus.PublishAsync(new TransferFailedEvent
        {
            TransferId = transfer.Id,
            FromAccountId = transfer.FromAccountId,
            ToAccountId = transfer.ToAccountId,
            Amount = transfer.Amount,
            Reason = @event.Reason
        });
    }
}
