namespace Transfer.Domain.Entities;

public class MoneyTransfer
{
    public Guid Id { get; set; }
    public Guid FromAccountId { get; set; }
    public Guid ToAccountId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "TRY";
    public TransferStatus Status { get; set; }
    public Guid InitiatedBy { get; set; }
    public DateTime InitiatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? FailureReason { get; set; }
}

public enum TransferStatus
{
    Pending,
    DebitSuccessful,
    Completed,
    Failed,
    Cancelled
}
