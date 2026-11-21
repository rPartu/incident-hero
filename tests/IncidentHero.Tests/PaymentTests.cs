namespace IncidentHero.Tests;

// Posts payments through payments-api and checks the result in ledger-api, the same path as the
// manual checks in requests.http.
public sealed class PaymentTests(AppHostFixture fixture) : IClassFixture<AppHostFixture>
{
    [Fact]
    public async Task Payment_is_added_to_the_account_balance()
    {
        // A fresh account per run, so the balance holds only this test's payment.
        var accountId = $"acc-{Guid.NewGuid()}";
        using var payments = fixture.App.CreateHttpClient("payments-api");
        using var ledger = fixture.App.CreateHttpClient("ledger-api");

        using var request = PaymentRequest(accountId, amountMinor: 1250);
        using var response = await payments.SendAsync(request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var payment = await response.Content.ReadFromJsonAsync<PaymentResponse>();
        Assert.NotEqual(Guid.Empty, payment!.PaymentId);

        var balance = await ledger.GetFromJsonAsync<BalanceResponse>($"/accounts/{accountId}/balance");
        Assert.Equal(1250, balance!.Balances["GBP"]);
    }

    [Fact]
    public async Task Zero_amount_payment_is_rejected()
    {
        using var payments = fixture.App.CreateHttpClient("payments-api");

        using var request = PaymentRequest("acc-1", amountMinor: 0);
        using var response = await payments.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static HttpRequestMessage PaymentRequest(string accountId, long amountMinor) =>
        new(HttpMethod.Post, "/payments")
        {
            Headers = { { "Idempotency-Key", Guid.NewGuid().ToString() } },
            Content = JsonContent.Create(new { accountId, amountMinor, currency = "GBP" }),
        };

    private sealed record PaymentResponse(Guid PaymentId);

    private sealed record BalanceResponse(string AccountId, Dictionary<string, long> Balances);
}
