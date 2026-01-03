namespace Account.Domain.Entities;

public class BankAccount
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string AccountNumber { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public string Currency { get; set; } = "TRY";
    public DateTime CreatedAt { get; set; }

    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}

public class Transaction
{
    public Guid Id { get; set; }
    public Guid AccountId { get; set; }
    public TransactionType Type { get; set; }
    public decimal Amount { get; set; }
    public Guid? TransferId { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }

    public BankAccount Account { get; set; } = null!;
}

public enum TransactionType
{
    Credit,
    Debit
}
