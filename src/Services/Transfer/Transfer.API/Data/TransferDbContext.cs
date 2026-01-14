using MassTransit;
using Microsoft.EntityFrameworkCore;
using Transfer.Domain.Entities;
using Transfer.Domain.Saga;

namespace Transfer.API.Data;

public class TransferDbContext : DbContext
{
    public TransferDbContext(DbContextOptions<TransferDbContext> options) : base(options)
    {
    }

    public DbSet<MoneyTransfer> Transfers { get; set; }
    public DbSet<TransferSagaState> TransferSagaStates { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<MoneyTransfer>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.FromAccountId);
            entity.HasIndex(e => e.ToAccountId);
            entity.HasIndex(e => e.InitiatedBy);
            entity.Property(e => e.Amount).HasPrecision(18, 2);
            entity.Property(e => e.Currency).HasMaxLength(3);
            entity.Property(e => e.FailureReason).HasMaxLength(500);
            entity.Property(e => e.InitiatedAt).HasColumnType("timestamp without time zone");
            entity.Property(e => e.CompletedAt).HasColumnType("timestamp without time zone");
        });

        // Configure Saga State Machine persistence
        modelBuilder.Entity<TransferSagaState>(entity =>
        {
            entity.HasKey(e => e.CorrelationId);
            entity.Property(e => e.CurrentState).HasMaxLength(64);
            entity.Property(e => e.Currency).HasMaxLength(3);
            entity.Property(e => e.Amount).HasPrecision(18, 2);
            entity.Property(e => e.FailureReason).HasMaxLength(500);
            entity.HasIndex(e => e.TransferId);
            entity.Property(e => e.InitiatedAt).HasColumnType("timestamp without time zone");
            entity.Property(e => e.DebitRequestedAt).HasColumnType("timestamp without time zone");
            entity.Property(e => e.DebitedAt).HasColumnType("timestamp without time zone");
            entity.Property(e => e.CreditRequestedAt).HasColumnType("timestamp without time zone");
            entity.Property(e => e.CreditedAt).HasColumnType("timestamp without time zone");
            entity.Property(e => e.CompletedAt).HasColumnType("timestamp without time zone");
            entity.Property(e => e.FailedAt).HasColumnType("timestamp without time zone");
            entity.Property(e => e.CompensationStartedAt).HasColumnType("timestamp without time zone");
            entity.Property(e => e.CompensationCompletedAt).HasColumnType("timestamp without time zone");
        });
    }
}
