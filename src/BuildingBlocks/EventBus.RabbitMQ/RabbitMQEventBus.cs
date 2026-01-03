using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace EventBus.RabbitMQ;

/// <summary>
/// RabbitMQ implementation of the event bus
/// </summary>
public class RabbitMQEventBus : IEventBus, IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RabbitMQEventBus> _logger;
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly string _exchangeName;
    private readonly Dictionary<Type, Type> _eventHandlers;

    public RabbitMQEventBus(
        string hostname,
        string exchangeName,
        IServiceProvider serviceProvider,
        ILogger<RabbitMQEventBus> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _exchangeName = exchangeName;
        _eventHandlers = new Dictionary<Type, Type>();

        var factory = new ConnectionFactory
        {
            HostName = hostname,
            DispatchConsumersAsync = true,
            AutomaticRecoveryEnabled = true,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();
        
        // Declare exchange
        _channel.ExchangeDeclare(_exchangeName, ExchangeType.Topic, durable: true, autoDelete: false);

        _logger.LogInformation("RabbitMQ EventBus initialized with exchange: {ExchangeName}", _exchangeName);
    }

    public async Task PublishAsync<TEvent>(TEvent @event) where TEvent : IntegrationEvent
    {
        var eventName = typeof(TEvent).Name;
        var message = JsonConvert.SerializeObject(@event);
        var body = Encoding.UTF8.GetBytes(message);

        var properties = _channel.CreateBasicProperties();
        properties.Persistent = true;
        properties.MessageId = @event.Id.ToString();
        properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());

        _channel.BasicPublish(
            exchange: _exchangeName,
            routingKey: eventName,
            basicProperties: properties,
            body: body);

        _logger.LogInformation("Published event {EventName} with ID {EventId}", eventName, @event.Id);

        await Task.CompletedTask;
    }

    public void Subscribe<TEvent, THandler>()
        where TEvent : IntegrationEvent
        where THandler : IIntegrationEventHandler<TEvent>
    {
        var eventName = typeof(TEvent).Name;
        var handlerType = typeof(THandler);

        _eventHandlers[typeof(TEvent)] = handlerType;

        var queueName = $"{eventName}_Queue";
        
        _channel.QueueDeclare(
            queue: queueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        _channel.QueueBind(
            queue: queueName,
            exchange: _exchangeName,
            routingKey: eventName);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.Received += async (sender, eventArgs) =>
        {
            var eventData = Encoding.UTF8.GetString(eventArgs.Body.ToArray());
            
            try
            {
                await ProcessEventAsync(eventName, eventData);
                _channel.BasicAck(eventArgs.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing event {EventName}", eventName);
                _channel.BasicNack(eventArgs.DeliveryTag, multiple: false, requeue: true);
            }
        };

        _channel.BasicConsume(queue: queueName, autoAck: false, consumer: consumer);

        _logger.LogInformation("Subscribed to event {EventName} with handler {HandlerName}", 
            eventName, handlerType.Name);
    }

    private async Task ProcessEventAsync(string eventName, string eventData)
    {
        using var scope = _serviceProvider.CreateScope();
        
        var eventType = _eventHandlers.Keys.FirstOrDefault(t => t.Name == eventName);
        if (eventType == null)
        {
            _logger.LogWarning("No event type found for event name: {EventName}", eventName);
            return;
        }

        var @event = JsonConvert.DeserializeObject(eventData, eventType);
        if (@event == null)
        {
            _logger.LogWarning("Failed to deserialize event: {EventName}", eventName);
            return;
        }

        var handlerType = _eventHandlers[eventType];
        var handler = scope.ServiceProvider.GetService(handlerType);
        
        if (handler == null)
        {
            _logger.LogWarning("No handler registered for event: {EventName}", eventName);
            return;
        }

        var handleMethod = handlerType.GetMethod(nameof(IIntegrationEventHandler<IntegrationEvent>.HandleAsync));
        if (handleMethod != null)
        {
            await (Task)handleMethod.Invoke(handler, new[] { @event })!;
        }
    }

    public void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
        _logger.LogInformation("RabbitMQ EventBus disposed");
    }
}
