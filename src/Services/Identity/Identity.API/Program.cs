using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Common.Logging;
using Identity.API.Data;
using Identity.API.Services;

// Fix PostgreSQL DateTime UTC issue
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
builder.Host.UseCustomSerilog("Identity.API");

// DEBUG: Print configuration to diagnose Docker issue
Console.WriteLine("========== CONFIGURATION DEBUG ==========");
Console.WriteLine($"Environment: {builder.Environment.EnvironmentName}");
Console.WriteLine($"Connection String from Config: {builder.Configuration.GetConnectionString("DefaultConnection")}");
Console.WriteLine($"JWT SecretKey exists: {!string.IsNullOrEmpty(builder.Configuration["JwtSettings:SecretKey"])}");
Console.WriteLine($"Environment Variables:");
Console.WriteLine($"  ConnectionStrings__DefaultConnection: {Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")}");
Console.WriteLine($"  RabbitMQ__Host: {Environment.GetEnvironmentVariable("RabbitMQ__Host")}");
Console.WriteLine("=========================================");

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
Console.WriteLine($"Using Connection String: {connectionString}");
builder.Services.AddDbContext<IdentityDbContext>(options =>
    options.UseNpgsql(connectionString));

// Services
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<ITokenService, TokenService>();

// JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey not configured");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
    };
});

builder.Services.AddAuthorization();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Apply migrations
Console.WriteLine("========== STARTING MIGRATION ==========");
using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        Console.WriteLine("DbContext created, attempting to connect to database...");
        
        var canConnect = db.Database.CanConnect();
        Console.WriteLine($"Can connect to database: {canConnect}");
        
        if (canConnect)
        {
            Console.WriteLine("Starting migration...");
            db.Database.Migrate();
            Console.WriteLine("Migration completed successfully!");
        }
        else
        {
            Console.WriteLine("ERROR: Cannot connect to database!");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"MIGRATION ERROR: {ex.Message}");
        Console.WriteLine($"Stack Trace: {ex.StackTrace}");
        // Don't crash, let the app start anyway
    }
}
Console.WriteLine("=========================================");

Console.WriteLine("🚀 Starting application...");
app.Run();

