using System.Reflection;
using System.Text.Json.Serialization;
using OutWit.Common.Licensing;
using OutWit.Common.Licensing.Binding;
using OutWit.Common.Licensing.Keys;
using OutWit.Common.Licensing.Snapshot;
using OutWit.Common.Licensing.Storage;
using OutWit.Common.Licensing.Validation;

// The WitCloud *shape* with none of the WitCloud *substance*: no database, no
// identity, no WitRPC. It exists to answer the questions the Avalonia bench
// cannot ask, and those are the ones whose failure mode is expensive — silent,
// delayed, and first noticed by a customer.
//
//   · does installId survive `docker compose up --force-recreate`
//   · does Licensing__License work beside a dropped .lic, and which wins
//   · does the store land somewhere a non-root container can write
//   · does a licence apply WITHOUT a restart
//   · do two containers produce two identities, and refuse each other's licence
//
// It can be destroyed and recreated in a loop, which is the point.

var builder = WebApplication.CreateBuilder(args);

// Modes and statuses as words. This is a diagnostic surface: an answer of
// "mode": 2 needs a decoder ring, and an instrument whose output has to be
// decoded is one people stop reading carefully.
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

var licensing = builder.Configuration.GetSection("Licensing");

var directory = licensing["Directory"] is { Length: > 0 } configured
    ? configured
    : Path.Combine(AppContext.BaseDirectory, "license");

// Configuration first, generated file as the fallback. An installer writes the
// id into .env at deploy time; everything this mock is for lives in the
// fallback path, which is what a hand-rolled `docker compose up` gets.
var installId = LicenseInstallId.Resolve(licensing["InstallId"], directory);

builder.Services.AddLicensing(options => options
    .ForProduct("SampleServer", Assembly.GetExecutingAssembly().GetName().Version)
    .WithKeyRing(LicenseKeyRing.FromJson(ReadKeyRing(licensing["KeyRing"])))
    .WithBinding(LicenseBindingProviderTenant.ForDeployment(
        installId,
        licensing["PublicBaseUrl"],
        licensing["Issuer"]))
    // Both doors at once, which is the arrangement worth testing: an operator
    // pastes into the admin screen, an installer drops a file, compose sets a
    // variable. Reads take all of them; writes go to the directory alone.
    .WithStore(new LicenseStoreComposite(
        new LicenseStoreFile(directory),
        new LicenseStoreEnvironment()))
    .WithDemo(TimeSpan.FromDays(30))
    // Without this a licence dropped into the volume is invisible until a
    // restart — and "applies without a restart" is a promise the design makes.
    .WithPeriodicReload(TimeSpan.FromSeconds(Seconds(licensing["ReloadSeconds"], 10)))
    .WithGrace(TimeSpan.FromDays(Days(licensing["GraceDays"], 14)))
    .Declares(vocabulary => vocabulary
        .Feature("accounting.export", "Accounting export")
        .Limit("maxNodes", "Compute nodes", 4)));

var app = builder.Build();

// Read per call, never cached in a field. That is the one rule that makes a
// licence pasted at 03:00 take effect without a restart.
app.MapGet("/health", (ILicenseService service) =>
{
    var snapshot = service.Snapshot;

    return Results.Json(new
    {
        status = snapshot.CanRun ? "healthy" : "degraded",
        licence = new
        {
            mode = snapshot.Mode.ToString(),
            status = snapshot.Status.ToString(),
            expires = snapshot.ExpiresUtc,
            daysRemaining = snapshot.DaysRemaining,
            customer = snapshot.CustomerName
        }
    });
});

app.MapGet("/license", (ILicenseService service) => Results.Json(service.Snapshot));

app.MapGet("/license/request", async (ILicenseService service) =>
    Results.Text(await service.CreateRequestAsync() is var request && request != null ? request.ToJson() : string.Empty,
        "application/json"));

app.MapPost("/license", async (HttpRequest http, ILicenseService service) =>
{
    using var reader = new StreamReader(http.Body);
    var token = (await reader.ReadToEndAsync()).Trim();

    var result = await service.InstallAsync(token);

    return result.IsValid || result.Status == LicenseStatus.NotYetValid
        ? Results.Json(new { accepted = true, status = result.Status.ToString(), message = result.Describe() })
        : Results.Json(new { accepted = false, status = result.Status.ToString(), message = result.Describe() },
            statusCode: StatusCodes.Status400BadRequest);
});

// The identity this deployment claims, so a test can assert that two containers
// are two installations without reading into the volume.
app.MapGet("/identity", () => Results.Json(new
{
    installId,
    publicBaseUrl = licensing["PublicBaseUrl"],
    issuer = licensing["Issuer"],
    directory
}));

// The gate. Two lines, which is the whole claim §7.1 makes about what licensing
// a product costs — and it reads State per call like everything else.
app.MapPost("/jobs", (ILicenseService service) =>
{
    if (!service.State.CanRun)
        return Results.Json(new { accepted = false, reason = service.State.Describe() }, statusCode: StatusCodes.Status402PaymentRequired);

    return Results.Json(new { accepted = true, maxNodes = service.Limit("maxNodes") });
});

app.Run();

return;

static string ReadKeyRing(string? value)
{
    if (string.IsNullOrWhiteSpace(value))
        return string.Empty;

    // Either the ring itself or a path to it, because a compose file passes one
    // and a mounted secret passes the other.
    return value!.TrimStart().StartsWith('{')
        ? value
        : File.Exists(value) ? File.ReadAllText(value) : string.Empty;
}

static int Seconds(string? value, int fallback) => int.TryParse(value, out var parsed) && parsed > 0 ? parsed : fallback;

static int Days(string? value, int fallback) => int.TryParse(value, out var parsed) && parsed >= 0 ? parsed : fallback;
