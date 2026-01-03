using EventBus.RabbitMQ;

namespace Notification.API.Events;

public class TransferCompletedEvent : IntegrationEvent
{
    public Guid TransferId { get; set; }
    public Guid FromAccountId { get; set; }
    public Guid ToAccountId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
}

public class TransferFailedEvent : IntegrationEvent
{
    public Guid TransferId { get; set; }
    public Guid FromAccountId { get; set; }
    public Guid ToAccountId { get; set; }
    public decimal Amount { get; set; }
    public string Reason { get; set; } = string.Empty;
}
