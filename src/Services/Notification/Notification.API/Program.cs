using Common.Logging;
using EventBus.MassTransit;
using Notification.API.EventHandlers;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
builder.Host.UseCustomSerilog("Notification.API");

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// MassTransit with RabbitMQ
builder.Services.AddMassTransitWithRabbitMQ(builder.Configuration, cfg =>
{
    // Register notification consumers
    cfg.AddConsumer<TransferCompletedConsumer>();
    cfg.AddConsumer<TransferFailedConsumer>();
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

app.Run();
