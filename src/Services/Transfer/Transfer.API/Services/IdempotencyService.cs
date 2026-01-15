using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Transfer.API.Data;
using Transfer.Domain.Entities;

namespace Transfer.API.Services;

/// <summary>
/// Service for handling idempotency keys to prevent duplicate transactions
/// </summary>
public interface IIdempotencyService
{
    Task<string?> GetCachedResponseAsync(string idempotencyKey);
    Task CacheResponseAsync(string idempotencyKey, object response, TimeSpan? expiry = null);
}

public class IdempotencyService : IIdempotencyService
{
    private readonly TransferDbContext _context;
    private readonly ILogger<IdempotencyService> _logger;

    public IdempotencyService(
        TransferDbContext context,
        ILogger<IdempotencyService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<string?> GetCachedResponseAsync(string idempotencyKey)
    {
        var request = await _context.IdempotentRequests
            .Where(i => i.IdempotencyKey == idempotencyKey && i.ExpiresAt > DateTime.UtcNow)
            .FirstOrDefaultAsync();

        if (request != null)
        {
            _logger.LogInformation("Idempotency key found: {Key}, returning cached response", idempotencyKey);
        }

        return request?.ResponseData;
    }

    public async Task CacheResponseAsync(string idempotencyKey, object response, TimeSpan? expiry = null)
    {
        var expiryTime = expiry ?? TimeSpan.FromHours(24);

        var request = new IdempotentRequest
        {
            Id = Guid.NewGuid(),
            IdempotencyKey = idempotencyKey,
            ResponseData = JsonSerializer.Serialize(response),
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.Add(expiryTime)
        };

        try
        {
            _context.IdempotentRequests.Add(request);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Cached response for idempotency key: {Key}", idempotencyKey);
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("unique") == true)
        {
            // Race condition - another request already cached this key
            _logger.LogWarning("Idempotency key already exists: {Key}", idempotencyKey);
        }
    }
}
