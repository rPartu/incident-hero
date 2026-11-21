using System.Collections.Concurrent;

namespace LedgerApi;

public sealed record LedgerEntry(Guid Id, string IdempotencyKey, string AccountId, long AmountMinor, string Currency);

// Where ledger entries live. In memory for now; add-ledger-database swaps in a Postgres version.
public interface ILedgerStore
{
    // Records the entry unless one already exists for its idempotency key.
    // Returns whichever entry is stored under that key: the new one, or the one recorded earlier.
    LedgerEntry AddOrGet(LedgerEntry entry);

    // Sum of the account's entries per currency. Empty when the account has no entries.
    IReadOnlyDictionary<string, long> GetBalances(string accountId);
}

public sealed class InMemoryLedgerStore : ILedgerStore
{
    private readonly ConcurrentDictionary<string, LedgerEntry> _entriesByKey = new();

    public LedgerEntry AddOrGet(LedgerEntry entry) => _entriesByKey.GetOrAdd(entry.IdempotencyKey, entry);

    public IReadOnlyDictionary<string, long> GetBalances(string accountId) =>
        _entriesByKey.Values
            .Where(e => e.AccountId == accountId)
            .GroupBy(e => e.Currency)
            .ToDictionary(g => g.Key, g => g.Sum(e => e.AmountMinor));
}
