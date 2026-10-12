using System.Data;
using DKP.Application.Persistence;
using Microsoft.EntityFrameworkCore;
namespace DKP.Infrastructure.Persistence;

/// <summary>Fresh context per operation; never share a tracked DbContext across Blazor circuit calls.</summary>
public sealed class CommandUnitOfWork(IDbContextFactory<DkpDbContext> factory) : ICommandUnitOfWork
{
    // All writes and replay acquire this transaction lock before reading state.
    // Deliberately coarse for one guild: correctness also covers catalog edits, blocking and role changes.
    internal const long LedgerLock = 73852912001;
    private readonly AsyncLocal<DkpDbContext?> current = new();
    public DkpDbContext Db => current.Value ?? throw new InvalidOperationException("A command transaction is required.");

    public async Task<T> ExecuteAsync<T>(IReadOnlyCollection<Guid> userIds, Func<Task<T>> action, CancellationToken ct = default)
    {
        if (current.Value != null) throw new InvalidOperationException("Nested command transactions are not supported.");
        await using var db = await factory.CreateDbContextAsync(ct);
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock({LedgerLock})", ct);
        foreach (var id in userIds.Distinct().Order())
            await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtextextended({id.ToString()}, 0))", ct);
        current.Value = db;
        try
        {
            var result = await action();
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return result;
        }
        finally
        {
            // On failure disposal rolls the whole transaction back; the context never gets reused.
            current.Value = null;
        }
    }
}
