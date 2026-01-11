using EventBus.MassTransit.Contracts;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Transfer.API.Data;
using Transfer.Domain.Entities;

namespace Transfer.API.EventHandlers;

/// <summary>
/// SAGA Step 2: Account debited successfully, now credit the receiver
/// </summary>
public class AccountDebitedConsumer : IConsumer<IAccountDebited>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<AccountDebitedConsumer> _logger;

    public AccountDebitedConsumer(
        IServiceProvider serviceProvider,
        IPublishEndpoint publishEndpoint,
        ILogger<AccountDebitedConsumer> logger)
    {
        _serviceProvider = serviceProvider;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<IAccountDebited> context)
    {
        var message = context.Message;
        
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TransferDbContext>();

        var transfer = await dbContext.Transfers.FindAsync(message.TransferId);
        if (transfer == null)
        {
            _logger.LogWarning("Transfer not found: {TransferId}", message.TransferId);
            return;
        }

        // Update SAGA state
        transfer.Status = TransferStatus.DebitSuccessful;
        await dbContext.SaveChangesAsync();

        _logger.LogInformation("Transfer {TransferId}: Debit successful, requesting credit", transfer.Id);

        // Publish credit request
        await _publishEndpoint.Publish<ICreditAccountRequested>(new
        {
            TransferId = transfer.Id,
            AccountId = transfer.ToAccountId,
            transfer.Amount
        });
    }
}

/// <summary>
/// SAGA Step 3: Account credited successfully, transfer complete!
/// </summary>
public class AccountCreditedConsumer : IConsumer<IAccountCredited>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<AccountCreditedConsumer> _logger;

    public AccountCreditedConsumer(
        IServiceProvider serviceProvider,
        IPublishEndpoint publishEndpoint,
        ILogger<AccountCreditedConsumer> logger)
    {
        _serviceProvider = serviceProvider;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<IAccountCredited> context)
    {
        var message = context.Message;
        
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TransferDbContext>();

        var transfer = await dbContext.Transfers.FindAsync(message.TransferId);
        if (transfer == null)
        {
            _logger.LogWarning("Transfer not found: {TransferId}", message.TransferId);
            return;
        }

        // SAGA COMPLETE!
        transfer.Status = TransferStatus.Completed;
        transfer.CompletedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync();

        _logger.LogInformation("Transfer {TransferId}: COMPLETED! {Amount} {Currency} transferred", 
            transfer.Id, transfer.Amount, transfer.Currency);

        // Notify success
        await _publishEndpoint.Publish<ITransferCompleted>(new
        {
            TransferId = transfer.Id,
            FromAccountId = transfer.FromAccountId,
            ToAccountId = transfer.ToAccountId,
            transfer.Amount,
            transfer.Currency
        });
    }
}

/// <summary>
/// SAGA Compensation: Something failed, handle it
/// </summary>
public class AccountOperationFailedConsumer : IConsumer<IAccountOperationFailed>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<AccountOperationFailedConsumer> _logger;

    public AccountOperationFailedConsumer(
        IServiceProvider serviceProvider,
        IPublishEndpoint publishEndpoint,
        ILogger<AccountOperationFailedConsumer> logger)
    {
        _serviceProvider = serviceProvider;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<IAccountOperationFailed> context)
    {
        var message = context.Message;
        
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TransferDbContext>();

        var transfer = await dbContext.Transfers.FindAsync(message.TransferId);
        if (transfer == null)
        {
            _logger.LogWarning("Transfer not found: {TransferId}", message.TransferId);
            return;
        }

        _logger.LogWarning("Transfer {TransferId}: Operation failed - {Reason}", 
            transfer.Id, message.Reason);

        // If credit failed but debit was successful, compensate!
        if (message.OperationType == "Credit" && transfer.Status == TransferStatus.DebitSuccessful)
        {
            _logger.LogInformation("Transfer {TransferId}: Compensating debit", transfer.Id);
            
            await _publishEndpoint.Publish<ICompensateDebit>(new
            {
                TransferId = transfer.Id,
                AccountId = transfer.FromAccountId,
                transfer.Amount
            });
        }

        // Mark transfer as failed
        transfer.Status = TransferStatus.Failed;
        transfer.CompletedAt = DateTime.UtcNow;
        transfer.FailureReason = message.Reason;
        await dbContext.SaveChangesAsync();

        // Notify failure
        await _publishEndpoint.Publish<ITransferFailed>(new
        {
            TransferId = transfer.Id,
            FromAccountId = transfer.FromAccountId,
            ToAccountId = transfer.ToAccountId,
            transfer.Amount,
            Reason = message.Reason
        });
    }
}
