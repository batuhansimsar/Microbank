using EventBus.MassTransit.Contracts;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Transfer.API.Data;
using Transfer.Domain.Entities;

namespace Transfer.API.EventHandlers;

public sealed class TransferOutcomeConsumer(TransferDbContext db) :
    IConsumer<ITransferCompleted>, IConsumer<ITransferFailed>
{
    public Task Consume(ConsumeContext<ITransferCompleted> context) =>
        RecordOutcomeAsync(context.Message.TransferId, TransferStatus.Completed, null, context.CancellationToken);

    public Task Consume(ConsumeContext<ITransferFailed> context) =>
        RecordOutcomeAsync(context.Message.TransferId, TransferStatus.Failed, context.Message.Reason, context.CancellationToken);

    private Task RecordOutcomeAsync(Guid transferId, TransferStatus status, string? reason, CancellationToken cancellationToken)
    {
        var completedAt = DateTime.UtcNow;
        return db.Transfers
            .Where(t => t.Id == transferId && t.Status != TransferStatus.Completed && t.Status != TransferStatus.Failed)
            .ExecuteUpdateAsync(transfer => transfer
                .SetProperty(t => t.Status, status)
                .SetProperty(t => t.CompletedAt, completedAt)
                .SetProperty(t => t.FailureReason, reason), cancellationToken);
    }
}
