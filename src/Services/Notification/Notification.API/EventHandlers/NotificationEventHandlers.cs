using EventBus.MassTransit.Contracts;
using MassTransit;

namespace Notification.API.EventHandlers;

/// <summary>
/// Handles transfer completion notifications
/// </summary>
public class TransferCompletedConsumer : IConsumer<ITransferCompleted>
{
    private readonly ILogger<TransferCompletedConsumer> _logger;

    public TransferCompletedConsumer(ILogger<TransferCompletedConsumer> logger)
    {
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ITransferCompleted> context)
    {
        var message = context.Message;
        
        _logger.LogInformation("📧 NOTIFICATION: Transfer SUCCESSFUL!");
        _logger.LogInformation("   Transfer ID: {TransferId}", message.TransferId);
        _logger.LogInformation("   Amount: {Amount} {Currency}", message.Amount, message.Currency);
        _logger.LogInformation("   From Account: {FromAccountId}", message.FromAccountId);
        _logger.LogInformation("   To Account: {ToAccountId}", message.ToAccountId);

        // Mock: Send email/SMS to both parties
        _logger.LogInformation("📨 Sending success email to sender...");
        _logger.LogInformation("📨 Sending success email to receiver...");
        _logger.LogInformation("📱 Sending SMS notifications...");

        await Task.CompletedTask;
    }
}

/// <summary>
/// Handles transfer failure notifications
/// </summary>
public class TransferFailedConsumer : IConsumer<ITransferFailed>
{
    private readonly ILogger<TransferFailedConsumer> _logger;

    public TransferFailedConsumer(ILogger<TransferFailedConsumer> logger)
    {
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ITransferFailed> context)
    {
        var message = context.Message;
        
        _logger.LogWarning("⚠️  NOTIFICATION: Transfer FAILED!");
        _logger.LogWarning("   Transfer ID: {TransferId}", message.TransferId);
        _logger.LogWarning("   Amount: {Amount}", message.Amount);
        _logger.LogWarning("   Reason: {Reason}", message.Reason);
        _logger.LogWarning("   From Account: {FromAccountId}", message.FromAccountId);

        // Mock: Send failure notification to sender
        _logger.LogInformation("📨 Sending failure email to sender...");
        _logger.LogInformation("📱 Sending SMS alert...");

        await Task.CompletedTask;
    }
}
