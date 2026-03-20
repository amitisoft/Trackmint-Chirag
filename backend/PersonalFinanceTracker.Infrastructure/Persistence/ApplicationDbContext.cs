using Microsoft.EntityFrameworkCore;
using PersonalFinanceTracker.Domain.Entities;
using PersonalFinanceTracker.Domain.Enums;

namespace PersonalFinanceTracker.Infrastructure.Persistence;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<Budget> Budgets => Set<Budget>();
    public DbSet<Goal> Goals => Set<Goal>();
    public DbSet<RecurringTransaction> RecurringTransactions => Set<RecurringTransaction>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(x => x.Email).IsUnique();
            entity.Property(x => x.Email).HasMaxLength(255);
            entity.Property(x => x.DisplayName).HasMaxLength(120);
        });

        modelBuilder.Entity<Account>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(100);
            entity.Property(x => x.InstitutionName).HasMaxLength(120);
            entity.Property(x => x.Type).HasConversion<string>();
            entity.Property(x => x.OpeningBalance).HasPrecision(12, 2);
            entity.Property(x => x.CurrentBalance).HasPrecision(12, 2);
            entity.HasOne(x => x.User)
                .WithMany(x => x.Accounts)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(100);
            entity.Property(x => x.Type).HasConversion<string>();
            entity.Property(x => x.Color).HasMaxLength(20);
            entity.Property(x => x.Icon).HasMaxLength(50);
            entity.HasOne(x => x.User)
                .WithMany(x => x.Categories)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.Property(x => x.Type).HasConversion<string>();
            entity.Property(x => x.Amount).HasPrecision(12, 2);
            entity.Property(x => x.Merchant).HasMaxLength(200);
            entity.Property(x => x.PaymentMethod).HasMaxLength(50);
            entity.Property(x => x.Tags).HasColumnType("text[]");

            entity.HasOne(x => x.User)
                .WithMany(x => x.Transactions)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.Account)
                .WithMany(x => x.SourceTransactions)
                .HasForeignKey(x => x.AccountId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.DestinationAccount)
                .WithMany(x => x.DestinationTransactions)
                .HasForeignKey(x => x.DestinationAccountId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Category)
                .WithMany(x => x.Transactions)
                .HasForeignKey(x => x.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(x => x.RecurringTransaction)
                .WithMany(x => x.GeneratedTransactions)
                .HasForeignKey(x => x.RecurringTransactionId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Budget>(entity =>
        {
            entity.Property(x => x.Amount).HasPrecision(12, 2);
            entity.HasIndex(x => new { x.UserId, x.CategoryId, x.Month, x.Year }).IsUnique();
        });

        modelBuilder.Entity<Goal>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(120);
            entity.Property(x => x.TargetAmount).HasPrecision(12, 2);
            entity.Property(x => x.CurrentAmount).HasPrecision(12, 2);
            entity.Property(x => x.Status).HasConversion<string>();
            entity.Property(x => x.Icon).HasMaxLength(50);
            entity.Property(x => x.Color).HasMaxLength(20);
            entity.HasOne(x => x.LinkedAccount)
                .WithMany()
                .HasForeignKey(x => x.LinkedAccountId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<RecurringTransaction>(entity =>
        {
            entity.Property(x => x.Title).HasMaxLength(120);
            entity.Property(x => x.Type).HasConversion<string>();
            entity.Property(x => x.Frequency).HasConversion<string>();
            entity.Property(x => x.Amount).HasPrecision(12, 2);
            entity.HasOne(x => x.Account)
                .WithMany()
                .HasForeignKey(x => x.AccountId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.DestinationAccount)
                .WithMany()
                .HasForeignKey(x => x.DestinationAccountId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.Property(x => x.TokenHash).HasMaxLength(256);
            entity.Property(x => x.ReplacedByTokenHash).HasMaxLength(256);
        });

        modelBuilder.Entity<PasswordResetToken>(entity =>
        {
            entity.Property(x => x.TokenHash).HasMaxLength(256);
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.Property(x => x.Action).HasMaxLength(100);
            entity.Property(x => x.EntityType).HasMaxLength(100);
            entity.Property(x => x.MetadataJson).HasColumnType("jsonb");
        });

        base.OnModelCreating(modelBuilder);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var trackedEntries = ChangeTracker.Entries()
            .Where(entry => entry.Entity is not null && entry.Entity.GetType().IsSubclassOf(typeof(Domain.Common.BaseEntity)));

        foreach (var entry in trackedEntries)
        {
            var entity = (Domain.Common.BaseEntity)entry.Entity;
            entity.UpdatedAt = DateTime.UtcNow;

            if (entry.State == EntityState.Added)
            {
                entity.CreatedAt = DateTime.UtcNow;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
