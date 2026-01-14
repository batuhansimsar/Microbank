using MassTransit;

namespace Transfer.Domain.Saga;

public class TransferSagaState : SagaStateMachineInstance
{
    // MassTransit correlation ID (equals TransferId)
    public Guid CorrelationId { get; set; }
    
    // Current state of the saga (MassTransit will set to Initial on creation)
    public string CurrentState { get; set; } = null!;
    
    // Transfer details
    public Guid TransferId { get; set; }
    public Guid FromAccountId { get; set; }
    public Guid ToAccountId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "TRY";
    public Guid InitiatedBy { get; set; }
    
    // Timestamps for tracking saga progress
    public DateTime InitiatedAt { get; set; }
    public DateTime? DebitRequestedAt { get; set; }
    public DateTime? DebitedAt { get; set; }
    public DateTime? CreditRequestedAt { get; set; }
    public DateTime? CreditedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? FailedAt { get; set; }
    
    // Error handling
    public string? FailureReason { get; set; }
    public int RetryCount { get; set; }
    
    // Compensation tracking
    public bool CompensationRequired { get; set; }
    public DateTime? CompensationStartedAt { get; set; }
    public DateTime? CompensationCompletedAt { get; set; }
}
