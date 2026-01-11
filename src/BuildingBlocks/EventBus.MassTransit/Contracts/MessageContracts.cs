namespace EventBus.MassTransit.Contracts;

/// <summary>
/// Request to debit an account (sent by Transfer Service to Account Service)
/// </summary>
public interface IDebitAccountRequested
{
    Guid TransferId { get; }
    Guid AccountId { get; }
    decimal Amount { get; }
}

/// <summary>
/// Account successfully debited (sent by Account Service to Transfer Service)
/// </summary>
public interface IAccountDebited
{
    Guid TransferId { get; }
    Guid AccountId { get; }
    decimal Amount { get; }
}

/// <summary>
/// Request to credit an account (sent by Transfer Service to Account Service)
/// </summary>
public interface ICreditAccountRequested
{
    Guid TransferId { get; }
    Guid AccountId { get; }
    decimal Amount { get; }
}

/// <summary>
/// Account successfully credited (sent by Account Service to Transfer Service)
/// </summary>
public interface IAccountCredited
{
    Guid TransferId { get; }
    Guid AccountId { get; }
    decimal Amount { get; }
}

/// <summary>
/// Account operation failed (sent by Account Service to Transfer Service)
/// </summary>
public interface IAccountOperationFailed
{
    Guid TransferId { get; }
    Guid AccountId { get; }
    string Reason { get; }
    string OperationType { get; } // "Debit" or "Credit"
}

/// <summary>
/// Compensation event to refund a debited account (sent by Transfer Service to Account Service)
/// </summary>
public interface ICompensateDebit
{
    Guid TransferId { get; }
    Guid AccountId { get; }
    decimal Amount { get; }
}

/// <summary>
/// Transfer completed successfully (sent by Transfer Service to Notification Service)
/// </summary>
public interface ITransferCompleted
{
    Guid TransferId { get; }
    Guid FromAccountId { get; }
    Guid ToAccountId { get; }
    decimal Amount { get; }
    string Currency { get; }
}

/// <summary>
/// Transfer failed (sent by Transfer Service to Notification Service)
/// </summary>
public interface ITransferFailed
{
    Guid TransferId { get; }
    Guid FromAccountId { get; }
    Guid ToAccountId { get; }
    decimal Amount { get; }
    string Reason { get; }
}
