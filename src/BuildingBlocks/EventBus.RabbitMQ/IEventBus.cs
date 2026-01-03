namespace EventBus.RabbitMQ;

/// <summary>
/// Interface for publishing and subscribing to integration events
/// </summary>
public interface IEventBus
{
    /// <summary>
    /// Publish an event to the message bus
    /// </summary>
    Task PublishAsync<TEvent>(TEvent @event) where TEvent : IntegrationEvent;

    /// <summary>
    /// Subscribe to an event type
    /// </summary>
    void Subscribe<TEvent, THandler>()
        where TEvent : IntegrationEvent
        where THandler : IIntegrationEventHandler<TEvent>;
}

/// <summary>
/// Handler interface for integration events
/// </summary>
public interface IIntegrationEventHandler<in TEvent> where TEvent : IntegrationEvent
{
    Task HandleAsync(TEvent @event);
}
