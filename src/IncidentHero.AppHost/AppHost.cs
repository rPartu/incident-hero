var builder = DistributedApplication.CreateBuilder(args);

// The resource names below become service.name in all telemetry. Recorded snapshots and lessons
// depend on them, so don't rename them.
var ledger = builder.AddDotnetProject("ledger-api", "../LedgerApi/LedgerApi.csproj");

// WithReference injects ledger-api's address into payments-api's configuration, which is what
// service discovery resolves "https+http://ledger-api" from.
builder.AddDotnetProject("payments-api", "../PaymentsApi/PaymentsApi.csproj")
    .WithReference(ledger);

builder.Build().Run();
