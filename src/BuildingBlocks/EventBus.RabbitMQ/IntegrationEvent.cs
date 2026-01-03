namespace EventBus.RabbitMQ;

/// <summary>
/// Base class for all integration events
/// </summary>
public abstract class IntegrationEvent
{
    public Guid Id { get; }
    public DateTime CreatedAt { get; }

    protected IntegrationEvent()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
    }
}
