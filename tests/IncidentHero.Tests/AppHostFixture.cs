using Aspire.Hosting;

namespace IncidentHero.Tests;

// Starts the real AppHost (payments-api and ledger-api) once and shares it with every test in a
// class, because starting it takes several seconds.
public sealed class AppHostFixture : IAsyncLifetime
{
    private static readonly TimeSpan StartTimeout = TimeSpan.FromMinutes(2);

    public DistributedApplication App { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.IncidentHero_AppHost>();
        App = await appHost.BuildAsync().WaitAsync(StartTimeout);
        await App.StartAsync().WaitAsync(StartTimeout);

        await App.ResourceNotifications.WaitForResourceHealthyAsync("ledger-api").WaitAsync(StartTimeout);
        await App.ResourceNotifications.WaitForResourceHealthyAsync("payments-api").WaitAsync(StartTimeout);
    }

    public async Task DisposeAsync() => await App.DisposeAsync();
}
