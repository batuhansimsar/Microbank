using EventBus.RabbitMQ;
using Notification.API.Events;

namespace Notification.API.EventHandlers;

public class TransferCompletedEventHandler : IIntegrationEventHandler<TransferCompletedEvent>
{
    private readonly ILogger<TransferCompletedEventHandler> _logger;

    public TransferCompletedEventHandler(ILogger<TransferCompletedEventHandler> logger)
    {
        _logger = logger;
    }

    public async Task HandleAsync(TransferCompletedEvent @event)
    {
        _logger.LogInformation("📧 NOTIFICATION: Transfer SUCCESSFUL!");
        _logger.LogInformation("   Transfer ID: {TransferId}", @event.TransferId);
        _logger.LogInformation("   Amount: {Amount} {Currency}", @event.Amount, @event.Currency);
        _logger.LogInformation("   From Account: {FromAccountId}", @event.FromAccountId);
        _logger.LogInformation("   To Account: {ToAccountId}", @event.ToAccountId);

        // Mock: Send email/SMS to both parties
        _logger.LogInformation("📨 Sending success email to sender...");
        _logger.LogInformation("📨 Sending success email to receiver...");
        _logger.LogInformation("📱 Sending SMS notifications...");

        await Task.CompletedTask;
    }
}

public class TransferFailedEventHandler : IIntegrationEventHandler<TransferFailedEvent>
{
    private readonly ILogger<TransferFailedEventHandler> _logger;

    public TransferFailedEventHandler(ILogger<TransferFailedEventHandler> logger)
    {
        _logger = logger;
    }

    public async Task HandleAsync(TransferFailedEvent @event)
    {
        _logger.LogWarning("⚠️  NOTIFICATION: Transfer FAILED!");
        _logger.LogWarning("   Transfer ID: {TransferId}", @event.TransferId);
        _logger.LogWarning("   Amount: {Amount}", @event.Amount);
        _logger.LogWarning("   Reason: {Reason}", @event.Reason);
        _logger.LogWarning("   From Account: {FromAccountId}", @event.FromAccountId);

        // Mock: Send failure notification to sender
        _logger.LogInformation("📨 Sending failure email to sender...");
        _logger.LogInformation("📱 Sending SMS alert...");

        await Task.CompletedTask;
    }
}
