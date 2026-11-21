using System.Net;

namespace PaymentsApi;

// Calls ledger-api. The base address is set to the service discovery name in Program.cs, and
// ServiceDefaults adds the standard resilience handler (timeouts, retries, circuit breaker).
public sealed class LedgerClient(HttpClient http)
{
    // Records an entry and returns its ID, or null when ledger-api says the idempotency key was
    // already used for a different entry (409). Any other failure throws.
    public async Task<Guid?> RecordEntryAsync(
        string idempotencyKey, string accountId, long amountMinor, string currency, CancellationToken cancellationToken)
    {
        var request = new LedgerEntryRequest(idempotencyKey, accountId, amountMinor, currency);
        using var response = await http.PostAsJsonAsync("/entries", request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.Conflict)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();

        var entry = await response.Content.ReadFromJsonAsync<LedgerEntryResponse>(cancellationToken);
        return entry!.EntryId;
    }

    private sealed record LedgerEntryRequest(string IdempotencyKey, string AccountId, long AmountMinor, string Currency);

    private sealed record LedgerEntryResponse(Guid EntryId);
}
