using EventBus.MassTransit.Contracts;
using MassTransit;

namespace Transfer.Domain.Saga;

/// <summary>
/// State machine that orchestrates the entire transfer saga flow
/// Handles: Debit → Credit → Completion or Compensation on failure
/// </summary>
public class TransferStateMachine : MassTransitStateMachine<TransferSagaState>
{
    // Define all possible states
    public State Initiated { get; private set; } = null!;
    public State Debiting { get; private set; } = null!;
    public State Debited { get; private set; } = null!;
    public State Crediting { get; private set; } = null!;
    public State Compensating { get; private set; } = null!;
    public State Completed { get; private set; } = null!;
    public State Failed { get; private set; } = null!;
    
    // Define events that drive state transitions
    public Event<ITransferInitiated> TransferInitiated { get; private set; } = null!;
    public Event<IAccountDebited> AccountDebited { get; private set; } = null!;
    public Event<IAccountCredited> AccountCredited { get; private set; } = null!;
    public Event<IAccountOperationFailed> AccountOperationFailed { get; private set; } = null!;
    
    public TransferStateMachine()
    {
        // Define which property is used to track current state
        InstanceState(x => x.CurrentState);
        
        // Configure event correlation (how to match events to saga instances)
        Event(() => TransferInitiated, x => x.CorrelateById(context => context.Message.TransferId));
        Event(() => AccountDebited, x => x.CorrelateById(context => context.Message.TransferId));
        Event(() => AccountCredited, x => x.CorrelateById(context => context.Message.TransferId));
        Event(() => AccountOperationFailed, x => x.CorrelateById(context => context.Message.TransferId));
        
        // INITIAL STATE: Transfer initiated
        Initially(
            When(TransferInitiated)
                .Then(context =>
                {
                    context.Saga.TransferId = context.Message.TransferId;
                    context.Saga.FromAccountId = context.Message.FromAccountId;
                    context.Saga.ToAccountId = context.Message.ToAccountId;
                    context.Saga.Amount = context.Message.Amount;
                    context.Saga.Currency = context.Message.Currency;
                    context.Saga.InitiatedBy = context.Message.InitiatedBy;
                    context.Saga.InitiatedAt = DateTime.UtcNow;
                    context.Saga.RetryCount = 0;
                    
                    Console.WriteLine($"[SAGA] Transfer {context.Saga.TransferId} initiated");
                })
                .TransitionTo(Initiated)
                .PublishAsync(context => context.Init<IDebitAccountRequested>(new
                {
                    TransferId = context.Saga.TransferId,
                    AccountId = context.Saga.FromAccountId,
                    Amount = context.Saga.Amount
                }))
                .Then(context =>
                {
                    context.Saga.DebitRequestedAt = DateTime.UtcNow;
                    Console.WriteLine($"[SAGA] Debit requested for transfer {context.Saga.TransferId}");
                })
                .TransitionTo(Debiting)
        );
        
        // STATE: Debiting - waiting for debit confirmation
        During(Debiting,
            // Success: Account debited
            When(AccountDebited)
                .Then(context =>
                {
                    context.Saga.DebitedAt = DateTime.UtcNow;
                    Console.WriteLine($"[SAGA] Account debited for transfer {context.Saga.TransferId}");
                })
                .TransitionTo(Debited)
                .PublishAsync(context => context.Init<ICreditAccountRequested>(new
                {
                    TransferId = context.Saga.TransferId,
                    AccountId = context.Saga.ToAccountId,
                    Amount = context.Saga.Amount
                }))
                .Then(context =>
                {
                    context.Saga.CreditRequestedAt = DateTime.UtcNow;
                    Console.WriteLine($"[SAGA] Credit requested for transfer {context.Saga.TransferId}");
                })
                .TransitionTo(Crediting),
            
            // Failure: Debit failed (insufficient balance, account not found, etc.)
            When(AccountOperationFailed)
                .If(context => context.Message.OperationType == "Debit", x => x
                    .Then(context =>
                    {
                        context.Saga.FailureReason = context.Message.Reason;
                        context.Saga.FailedAt = DateTime.UtcNow;
                        Console.WriteLine($"[SAGA] Debit failed for transfer {context.Saga.TransferId}: {context.Message.Reason}");
                    })
                    .PublishAsync(context => context.Init<ITransferFailed>(new
                    {
                        TransferId = context.Saga.TransferId,
                        FromAccountId = context.Saga.FromAccountId,
                        ToAccountId = context.Saga.ToAccountId,
                        Amount = context.Saga.Amount,
                        Reason = context.Saga.FailureReason ?? "Unknown error"
                    }))
                    .TransitionTo(Failed)
                    .Finalize()
                )
        );
        
        // STATE: Crediting - waiting for credit confirmation
        During(Crediting,
            // Success: Account credited - SAGA COMPLETE!
            When(AccountCredited)
                .Then(context =>
                {
                    context.Saga.CreditedAt = DateTime.UtcNow;
                    context.Saga.CompletedAt = DateTime.UtcNow;
                    Console.WriteLine($"[SAGA] Transfer {context.Saga.TransferId} COMPLETED successfully!");
                })
                .PublishAsync(context => context.Init<ITransferCompleted>(new
                {
                    TransferId = context.Saga.TransferId,
                    FromAccountId = context.Saga.FromAccountId,
                    ToAccountId = context.Saga.ToAccountId,
                    Amount = context.Saga.Amount,
                    Currency = context.Saga.Currency
                }))
                .TransitionTo(Completed)
                .Finalize(),
            
            // Failure: Credit failed - COMPENSATION REQUIRED!
            When(AccountOperationFailed)
                .If(context => context.Message.OperationType == "Credit", x => x
                    .Then(context =>
                    {
                        context.Saga.FailureReason = context.Message.Reason;
                        context.Saga.CompensationRequired = true;
                        context.Saga.CompensationStartedAt = DateTime.UtcNow;
                        Console.WriteLine($"[SAGA] Credit failed for transfer {context.Saga.TransferId}: {context.Message.Reason}. Starting compensation...");
                    })
                    .TransitionTo(Compensating)
                    .PublishAsync(context => context.Init<ICompensateDebit>(new
                    {
                        TransferId = context.Saga.TransferId,
                        AccountId = context.Saga.FromAccountId,
                        Amount = context.Saga.Amount
                    }))
                    .Then(context =>
                    {
                        context.Saga.CompensationCompletedAt = DateTime.UtcNow;
                        context.Saga.FailedAt = DateTime.UtcNow;
                        Console.WriteLine($"[SAGA] Compensation completed for transfer {context.Saga.TransferId}");
                    })
                    .PublishAsync(context => context.Init<ITransferFailed>(new
                    {
                        TransferId = context.Saga.TransferId,
                        FromAccountId = context.Saga.FromAccountId,
                        ToAccountId = context.Saga.ToAccountId,
                        Amount = context.Saga.Amount,
                        Reason = context.Saga.FailureReason ?? "Unknown error"
                    }))
                    .TransitionTo(Failed)
                    .Finalize()
                )
        );
        
        // Configure what happens when saga is finalized
        SetCompletedWhenFinalized();
    }
}
