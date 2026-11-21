using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using PaymentsApi;
using Polly;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddProblemDetails();
builder.Services.AddValidation();

// "https+http://ledger-api" is a service discovery name, not a real host: it resolves to the
// address the AppHost injects for ledger-api, preferring https.
builder.Services.AddHttpClient<LedgerClient>(client => client.BaseAddress = new Uri("https+http://ledger-api"));

var app = builder.Build();

app.MapDefaultEndpoints();

// Accepts a payment only after ledger-api has recorded it. The client's Idempotency-Key is passed
// to the ledger unchanged, so resubmitting the same payment returns the same payment ID instead
// of recording it twice. The payment ID is the ledger entry ID.
app.MapPost("/payments", async (
    [FromHeader(Name = "Idempotency-Key"), Required(ErrorMessage = "The Idempotency-Key header is required.")] string? idempotencyKey,
    PaymentRequest payment,
    LedgerClient ledger,
    ILogger<Program> logger,
    CancellationToken cancellationToken) =>
{
    Guid? paymentId;
    try
    {
        paymentId = await ledger.RecordEntryAsync(
            idempotencyKey!, payment.AccountId!, payment.AmountMinor, payment.Currency!, cancellationToken);
    }
    catch (Exception ex) when (ex is HttpRequestException or ExecutionRejectedException)
    {
        // HttpRequestException: ledger-api unreachable or a non-success status.
        // ExecutionRejectedException: the resilience handler gave up (timeout or open circuit).
        logger.LogError(ex, "Ledger could not record payment for account {AccountId}", payment.AccountId);
        return Results.Problem(
            statusCode: StatusCodes.Status502BadGateway,
            title: "Ledger unavailable",
            detail: "The payment was not recorded because ledger-api could not be reached.");
    }

    if (paymentId is null)
    {
        logger.LogWarning("Idempotency key {IdempotencyKey} reused for a different payment", idempotencyKey);
        return Results.Problem(
            statusCode: StatusCodes.Status409Conflict,
            title: "Idempotency key already used",
            detail: $"Idempotency key '{idempotencyKey}' was already used for a different payment.");
    }

    logger.LogInformation(
        "Payment {PaymentId} recorded: {AmountMinor} {Currency} to account {AccountId}",
        paymentId, payment.AmountMinor, payment.Currency, payment.AccountId);

    return Results.Created((string?)null, new PaymentResponse(paymentId.Value));
});

app.Run();

public sealed record PaymentRequest(
    [Required] string? AccountId,
    [Range(1, long.MaxValue, ErrorMessage = "AmountMinor must be positive.")] long AmountMinor,
    [Required, RegularExpression("^[A-Z]{3}$", ErrorMessage = "Currency must be a three-letter code.")] string? Currency);

public sealed record PaymentResponse(Guid PaymentId);
