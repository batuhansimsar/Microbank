using EventBus.RabbitMQ;

namespace Account.API.Events;

// Events this service publishes
public class AccountDebitedEvent : IntegrationEvent
{
    public Guid TransferId { get; set; }
    public Guid AccountId { get; set; }
    public decimal Amount { get; set; }
}

public class AccountCreditedEvent : IntegrationEvent
{
    public Guid TransferId { get; set; }
    public Guid AccountId { get; set; }
    public decimal Amount { get; set; }
}

public class AccountOperationFailedEvent : IntegrationEvent
{
    public Guid TransferId { get; set; }
    public Guid AccountId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string OperationType { get; set; } = string.Empty; // "Debit" or "Credit"
}

// Events this service consumes
public class DebitAccountRequestedEvent : IntegrationEvent
{
    public Guid TransferId { get; set; }
    public Guid AccountId { get; set; }
    public decimal Amount { get; set; }
}

public class CreditAccountRequestedEvent : IntegrationEvent
{
    public Guid TransferId { get; set; }
    public Guid AccountId { get; set; }
    public decimal Amount { get; set; }
}

public class CompensateDebitEvent : IntegrationEvent
{
    public Guid TransferId { get; set; }
    public Guid AccountId { get; set; }
    public decimal Amount { get; set; }
}
