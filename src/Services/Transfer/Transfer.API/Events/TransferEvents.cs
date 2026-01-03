using EventBus.RabbitMQ;

namespace Transfer.API.Events;

// Events published BY Transfer Service (SAGA Orchestrator)
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

public class TransferCompletedEvent : IntegrationEvent
{
    public Guid TransferId { get; set; }
    public Guid FromAccountId { get; set; }
    public Guid ToAccountId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
}

public class TransferFailedEvent : IntegrationEvent
{
    public Guid TransferId { get; set; }
    public Guid FromAccountId { get; set; }
    public Guid ToAccountId { get; set; }
    public decimal Amount { get; set; }
    public string Reason { get; set; } = string.Empty;
}

// Events consumed BY Transfer Service (from Account Service)
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
    public string OperationType { get; set; } = string.Empty;
}
