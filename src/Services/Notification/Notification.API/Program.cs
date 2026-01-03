using Common.Logging;
using EventBus.RabbitMQ;
using Notification.API.Events;
using Notification.API.EventHandlers;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
builder.Host.UseCustomSerilog("Notification.API");

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Event Bus
var rabbitMqHost = builder.Configuration["RabbitMQ:Host"] ?? "localhost";
builder.Services.AddRabbitMQEventBus(rabbitMqHost);

// Event Handlers
builder.Services.AddScoped<TransferCompletedEventHandler>();
builder.Services.AddScoped<TransferFailedEventHandler>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

// Subscribe to transfer events
using (var scope = app.Services.CreateScope())
{
    var eventBus = scope.ServiceProvider.GetRequiredService<IEventBus>();
    eventBus.Subscribe<TransferCompletedEvent, TransferCompletedEventHandler>();
    eventBus.Subscribe<TransferFailedEvent, TransferFailedEventHandler>();
}

app.Run();
