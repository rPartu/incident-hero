using System.ComponentModel.DataAnnotations;
using LedgerApi;
using Microsoft.AspNetCore.HttpLogging;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddProblemDetails();
builder.Services.AddValidation();

// Logs each request and response, bodies included, as one log record so they show up under
// Structured logs in the Aspire dashboard. Fine for the local lab; not something to keep in production.
builder.Services.AddHttpLogging(o =>
{
    o.LoggingFields = HttpLoggingFields.RequestPropertiesAndHeaders | HttpLoggingFields.RequestBody
                    | HttpLoggingFields.ResponseStatusCode | HttpLoggingFields.ResponseBody;
    o.RequestHeaders.Add("Idempotency-Key");
    o.CombineLogs = true;
});
builder.Services.AddSingleton<ILedgerStore, InMemoryLedgerStore>();

var app = builder.Build();

app.UseHttpLogging();

app.MapDefaultEndpoints();

// Records an entry. A repeat with the same idempotency key returns the entry recorded the first
// time; a repeat with the same key but a different account, amount or currency is a 409.
app.MapPost("/entries", (EntryRequest request, ILedgerStore store) =>
{
    var newEntry = new LedgerEntry(
        Guid.NewGuid(), request.IdempotencyKey!, request.AccountId!, request.AmountMinor, request.Currency!);

    var stored = store.AddOrGet(newEntry);

    var sameEntry = stored.AccountId == newEntry.AccountId
        && stored.AmountMinor == newEntry.AmountMinor
        && stored.Currency == newEntry.Currency;

    if (!sameEntry)
    {
        return Results.Problem(
            statusCode: StatusCodes.Status409Conflict,
            title: "Idempotency key already used",
            detail: $"Idempotency key '{request.IdempotencyKey}' was already used for a different entry.");
    }

    return Results.Created((string?)null, new EntryResponse(stored.Id));
});

app.MapGet("/accounts/{accountId}/balance", (string accountId, ILedgerStore store) =>
    Results.Ok(new BalanceResponse(accountId, store.GetBalances(accountId))));

app.Run();

public sealed record EntryRequest(
    [Required] string? IdempotencyKey,
    [Required] string? AccountId,
    [Range(1, long.MaxValue, ErrorMessage = "AmountMinor must be positive.")] long AmountMinor,
    [Required, RegularExpression("^[A-Z]{3}$")] string? Currency);

public sealed record EntryResponse(Guid EntryId);

public sealed record BalanceResponse(string AccountId, IReadOnlyDictionary<string, long> Balances);
