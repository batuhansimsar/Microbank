namespace Transfer.Domain.Entities;

/// <summary>
/// Tracks processed idempotency keys to prevent duplicate transactions
/// </summary>
public class IdempotentRequest
{
    public Guid Id { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public string ResponseData { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
}
