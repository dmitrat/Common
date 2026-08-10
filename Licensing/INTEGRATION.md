# Licensing integration

How to put licensing into a product. Three documents sit beside each other and
answer different questions:

| Document | Answers |
|---|---|
| [`DESIGN.md`](DESIGN.md) | What a licence *is* — the token format, the rules, the cryptography |
| [`ENFORCEMENT.md`](ENFORCEMENT.md) | Why the decisions are what they are, and what was rejected |
| **this** | What you actually type, in what order, to license a product |

`WitLicense/DESIGN.md` covers the other side of the transaction: the service that
issues.

---

## 1. What exists today

Three packages on nuget.org, all published, all consumed the same way as the rest
of the `OutWit.Common` family.

| Package | Version | What it holds |
|---|---|---|
| `OutWit.Common.Licensing` | 1.1.0 | Verification, state, storage, binding, requests. **No UI, no container requirement** |
| `OutWit.Common.Licensing.MVVM` | 1.0.0 | `ILicenseGateway`, `LicenseGatewayLocal`, `LicensePanelViewModel<T>`, three platform seams. **No UI framework dependency** — not Avalonia, not WPF, not Blazor |
| `OutWit.Common.Licensing.Generator` | 1.0.0 | Analyzer. Turns `*.product.json` into compile-checked keys and `*.keyring.json` into a tamper-resistant constant |

And one service:

| | |
|---|---|
| `WitLicense` | 1.2.1, deployed at `license.omnibuscloud.com`. Issues, delivers by email, exports key rings, keeps the catalogue and the audit trail |

Two reference implementations live in [`Samples/`](Samples/) and are meant to be
read before writing a third:

- **`OutWit.Common.Licensing.Samples.Avalonia`** — the desktop shape. Also the
  bench: it can issue throwaway licences to itself, travel through time, and load
  a real exported key ring.
- **`OutWit.Common.Licensing.Samples.Server`** — the service shape, in Docker,
  with two containers proving they are two installations.

---

## 2. The shape of every integration

Five things, in this order, regardless of family. Nothing else is required and
nothing else should be invented.

1. **Declare the vocabulary** — one `*.product.json`, committed.
2. **Carry the key ring** — one `*.keyring.json`, exported by WitLicense.
3. **Compose the service** — one call, either `Licensing.Create(...)` or
   `AddLicensing(...)`.
4. **Gate exactly one thing, by hand.** This is the part that is deliberately not
   generated, not shared and not clever.
5. **Show a screen** — `LicensePanelViewModel<T>` behind a view.

Steps 1, 2, 3 and 5 are mechanical. Step 4 is where the product's judgement
lives, and `ENFORCEMENT.md` §2 is the argument for keeping it hand-written: six
products refuse six different things for six different reasons, and a generic
refusal is the one thing the design forbids.

---

## 3. The two files

### 3.1 `<product>.product.json` — the vocabulary

```jsonc
{
  "product": "WitSweep",
  "features": [
    { "key": "format.nas", "name": "Nastran decks" },
    { "key": "integration.prepomax", "name": "PrePoMax integration" }
  ],
  "limits": [
    { "key": "maxVariants", "name": "Variants per sweep", "default": 64 }
  ]
}
```

Out comes `WitSweepLicense` with `Product`, `Features.FormatNas`,
`Limits.MaxVariants` and a `Declare(LicenseVocabulary)`:

```csharp
options.Declares(WitSweepLicense.Declare);

if (!licensing.HasFeature(WitSweepLicense.Features.FormatNas))
    return Refuse("This licence does not cover Nastran decks.");
```

This removes the one hazard nothing else catches. `HasFeature("format.nass")`
compiles, runs, and quietly disables a capability the customer paid for — no
error, no warning, and nothing in the unrecognised-key report, because that
report only ever sees what the *licence* granted, never what the *code* asked
for.

The same file is what the registry imports, so the catalogue entry stops being
hand-typed.

### 3.2 `<product>.keyring.json` — the trusted keys

Exported by WitLicense, committed. Out comes `WitSweepKeyRing.Create()` holding
the keys as a `const string`:

```csharp
options.WithKeyRing(WitSweepKeyRing.Create());
```

Never an embedded resource. Substitution — swapping the trusted public key for
your own — is the cheaper attack on an offline verifier and the one that yields a
binary that *genuinely validates* every licence its new owner mints. A resource
is a blob in the assembly manifest, findable with `strings`, replaceable without
touching an instruction, and not a literal in IL, so string encryption does not
cover it. A constant is the same data in the shape an obfuscator can defend.

`<product>.dev.keyring.json` is optional and emits under `#if DEBUG`, so a
development licence is worthless against a shipped build with no runtime check
enforcing it.

### 3.3 Wiring the generator

```xml
<PackageReference Include="OutWit.Common.Licensing.Generator"
                  PrivateAssets="all" />
```

Both files are then picked up **by convention** — `**/*.product.json` and
`**/*.keyring.json` — from the package's build props. Nothing to register. A
product that had to remember to register its own vocabulary is one that will
eventually forget, and forgetting looks like an empty vocabulary rather than a
build break.

Diagnostics:

| Id | Severity | Meaning |
|---|---|---|
| `OWL001` | Error | The descriptor could not be read — reason and offset |
| `OWL002` | Error | The ring could not be read, or would trust less than it appears to |
| `OWL003` | Warning | The ring parses, but part of it trusts nothing |
| `OWL004` | Error | Two rings claim the same product |

`OWL001` and `OWL002` are errors on purpose. Falling back to an empty vocabulary
or an empty ring would reintroduce exactly the silent failure the generator
exists to remove.

---

## 4. Clients — desktop products

WitSweep, the Inventor add-in, and anything else with a window.

### 4.1 Composing

A desktop app composes by hand. There is no container, and none is needed:

```csharp
var licensing = Licensing.Create(options => options
    .ForProduct("WitSweep", SweepVersionInfo.Version, fingerprintPrefix: "WSW")
    .WithKeyRing(WitSweepKeyRing.Create())
    .WithBinding(new LicenseBindingProviderMachine())
    .WithStore(new LicenseStoreFile(directories.Config))
    .WithDemo(TimeSpan.FromDays(30), demo => demo
        .Limit(WitSweepLicense.Limits.MaxVariants, 8)
        .Feature(WitSweepLicense.Features.FormatNas))
    .Declares(WitSweepLicense.Declare)
    .WithGrace(TimeSpan.Zero));
```

`Licensing.Create` performs a synchronous first evaluation, which is safe only
because every `await` inside the package uses `ConfigureAwait(false)`. That is
not incidental — the harness found the deadlock once already. Do not remove it,
and do not assume it in a new code path without checking.

If the host does have an `IServiceCollection`, `AddLicensing(...)` has the same
options builder and registers `ILicenseService` by factory so the container
disposes it.

### 4.2 Binding

`LicenseBindingProviderMachine` supplies the factors; the **threshold lives in
the licence**, chosen by the issuer, not by the product. Machine binding is
issued as 2-of-3 because hardware drifts — a replaced disk must not invalidate a
licence — with a fallback of 1 for hosts that can only produce one factor.

### 4.3 The screen

`LicensePanelViewModel<TApplicationVm>` is **held, never inherited from**, and
computes everything a licence screen needs. See §6 for what belongs on it.

```csharp
var gateway = new LicenseGatewayLocal(licensing);

Panel = new LicensePanelViewModel<ApplicationViewModel>(
    ApplicationVm,
    gateway,
    new LicenseClipboardAvalonia(),
    new LicenseFileTransferAvalonia(),
    AvaloniaDispatcher.UIThread);
```

**The dispatcher is not optional decoration.** `StateChanged` is raised from a
pool thread — the library suppresses the synchronization context by design so a
desktop host can block on the first evaluation without deadlocking — so a
snapshot pushed by a periodic re-evaluation arrives on a timer thread and would
touch bound collections off the UI thread. This cost the harness a day and a
misdiagnosis; supply all three seams.

### 4.4 The gate

Two conditions, adjacent, deliberately **not merged**:

```csharp
private void UpdateStatus()
{
    CanRun = Session.IsSignedIn
             && ApplicationVm.Licensing.Mode is LicenseMode.Licensed
                                             or LicenseMode.Demo
                                             or LicenseMode.Grace;
}
```

And the refusal, which is where honesty is either kept or lost. A disabled
button that says nothing is not a refusal — it is a mystery. The licence refusal
must produce text, distinct from the sign-in one, and lead to the licence screen.

---

## 5. Servers — WitCloud, WitIdentity

### 5.1 Composing

```csharp
var installId = LicenseInstallId.Resolve(licensing["InstallId"], directory);

builder.Services.AddLicensing(options => options
    .ForProduct("WitCloud", version, fingerprintPrefix: "WCL")
    .WithKeyRing(WitCloudKeyRing.Create())
    .WithBinding(LicenseBindingProviderTenant.ForDeployment(
        installId,
        hosting.PublicBaseUrl,
        identity.Issuer))
    .WithStore(new LicenseStoreComposite(
        new LicenseStoreFile(directory),
        new LicenseStoreEnvironment()))
    .WithDemo(TimeSpan.FromDays(30))
    .WithPeriodicReload(TimeSpan.FromSeconds(10))
    .WithGrace(TimeSpan.FromDays(14))
    .Declares(WitCloudLicense.Declare));
```

### 5.2 What differs from a client, and why

**Binding is 3-of-3, not 2-of-3.** Deployments do not drift the way hardware
does: `installId`, `publicBaseUrl` and `issuer` are all stable, and requiring all
three is what makes a cloned volume useless. A clone keeps the `installId` but
cannot keep the URL and stay reachable, nor the issuer and still validate a
token. Proven with two containers: same image, same volume contents, different
address → refused. WitIdentity drops `issuer`, which for it is its own URL, and
is issued 2-of-2.

**`installId` is configuration first, file second.** `Licensing__InstallId` in
`.env`, written by the installer at deploy; `<licenceDir>/install-id` generated
as the fallback when it is unset. The difference matters and was measured: a
configured id survives total destruction of the volume, a generated one does not.

**Both doors at once.** `LicenseStoreComposite` reads a file *and* an environment
variable — an operator pastes into an admin screen, an installer drops a file, a
compose file sets a variable. Reads take the union; writes go to the primary
alone.

**`WithPeriodicReload` is required, not optional.** Without it, a licence dropped
into the volume is invisible until a restart, and "applies without a restart" is
a promise this design makes.

**Renewal grace is real here.** A service that goes dark at midnight on renewal
day is an outage caused by an invoice. 14 days is the family default; a desktop
product uses zero, because a desktop refusing to start is an inconvenience.

### 5.3 Surfacing it

`Snapshot` is a flat, serialisable projection — put it straight on an endpoint:

```csharp
app.MapGet("/health", (ILicenseService service) =>
{
    var snapshot = service.Snapshot;

    return Results.Json(new
    {
        status = snapshot.CanRun ? "healthy" : "degraded",
        licence = new { mode = snapshot.Mode.ToString(), expires = snapshot.ExpiresUtc }
    });
});
```

Serialise modes and statuses as **words**. An answer of `"mode": 2` needs a
decoder ring, and an instrument whose output has to be decoded is one people stop
reading carefully.

The gate is two lines, and `402 Payment Required` is the honest code:

```csharp
app.MapPost("/jobs", (ILicenseService service) =>
{
    if (!service.State.CanRun)
        return Results.Json(new { accepted = false, reason = service.State.Describe() },
                            statusCode: StatusCodes.Status402PaymentRequired);

    return Results.Json(new { accepted = true, maxNodes = service.Limit(WitCloudLicense.Limits.MaxNodes) });
});
```

---

## 6. What belongs on the licence screen

Every value below is already a property on `LicensePanelViewModel`. A product
computes none of them.

**Two audiences, two zones.** The user asks "can I run, and what do I do if
not". The person on the phone to support needs to read out codes. Do not merge
them.

| Zone | Properties | Purpose |
|---|---|---|
| Status | `ModeText`, `Severity`, `StatusDetail`, `IsClockSuspect` | The answer, in one sentence someone can act on |
| Entitlement | `Grants`, `Edition`, `Customer`, `ExpiryText`, `GracePolicyText`, `Warnings` | What was bought — and, in demo, what is being withheld |
| This machine | `Fingerprint`, `LicenseId`, `CopyFingerprintCmd` | Support. Both codes must be selectable, not merely displayed |
| Transaction | `PastedToken` + `InstallCmd`, `OpenLicenseFileCmd`, `RequestContact`/`RequestNotes` + `CreateRequestCmd`, `CopyRequestCmd`, `SaveRequestCmd` | Getting a licence in, and asking for one |
| Housekeeping | `Installed`, `SelectedDocument`, `RemoveCmd` | Usually one document; two during a renewal overlap |

Three rules the screen must obey:

- **Reachable in every mode, including `Restricted`.** It cannot be gated, and it
  cannot sit behind sign-in — otherwise the person with an expired licence cannot
  reach the form that renews it.
- **The panel never gates.** It publishes `CanRun`; deciding what to refuse is
  the product's job.
- **`GracePolicyText` is shown whether or not there is grace.** A promise
  disclosed only when it applies is a promise nobody planned around.

The banner is *not* part of this screen. It lives in the shell and leads here;
the licence screen is somewhere you go, not something that nags.

---

## 7. Rules that apply everywhere

- **Read `State` or `Snapshot` per call. Never cache them in a field.** That one
  rule is what makes a licence pasted at 03:00 take effect without a restart.
- **Never gate observation.** Watching a job that is already running, reading
  past results, exporting data the customer already produced — none of these are
  licensed work. A licence lapse must not hold data hostage.
- **Never gate sign-in.** Sign-in must work with no licence, and a licence must
  work with no sign-in.
- **A refusal names its cause.** `LicenseState.Describe()` and
  `LicenseValidationResult.Describe()` already produce the sentence; use them
  rather than writing a second vocabulary of excuses.
- **Declare the vocabulary.** Without `Declares(...)` the unknown-key report has
  nothing to compare against, and a licence granting `sso` to a build that has
  never heard of it passes in silence.

---

## 8. The life of a licence

| Step | Where | Artefact |
|---|---|---|
| 1. Customer asks | Product, licence screen | `CreateRequestAsync()` → `.owlreq` blob (fingerprint, factors, contact, notes) |
| 2. Vendor imports | WitLicense, Issue dialog | The request pre-fills the binding |
| 3. Vendor issues | WitLicense | Term, version range, features, limits; unlimited terms require a written reason |
| 4. Delivery | WitLicense → Resend | Email carrying the token as text, for pasting |
| 5. Install | Product | `InstallAsync(token)` — a token that does not validate is **not stored**, so a pasted mistake cannot displace a working licence |
| 6. Renewal | Both | The renewal is installed alongside the incumbent and takes over at its own `nbf`, not at the incumbent's `exp` |
| 7. Removal | Product | `RemoveAsync(jti)`. Returns false for a licence the product does not own — one supplied by an environment variable, say |

Steps 1 through 5 have been run end to end against the production service: a real
request from the bench, a real issuance, a real email through Resend, a real
paste, verified against a real exported key ring.

---

## 9. Checklist for a new product

1. Add `OutWit.Common.Licensing`; add `.MVVM` if it has a screen; add
   `.Generator` with `PrivateAssets="all"`.
2. Write `<product>.product.json`. Commit it.
3. Create the catalogue entry in WitLicense — importing the descriptor rather
   than typing the keys again.
4. Generate the key pair, export `<product>.keyring.json`, commit it.
5. Compose: `Licensing.Create(...)` for a desktop app, `AddLicensing(...)` for a
   service.
6. Pick the binding: machine 2-of-3 for a workstation, tenant 3-of-3 for a
   deployment.
7. Pick the demo, and pick the grace — zero for desktop, 14 days for a service.
8. Write the gate. One place. By hand. With a sentence.
9. Build the screen from `LicensePanelViewModel<T>`, supplying **all three**
   seams.
10. Issue a licence to yourself and install it before calling it done.

---

## 10. What is proven, and what is not

Stated plainly, because "it compiles" and "it works" are different claims.

| | Status |
|---|---|
| Verification, state, modes, storage, binding | 201 tests, three TFMs |
| Panel view model, gateway, thread affinity | 34 tests |
| Both generators, compiled against the real library | 20 tests, both sides of `#if DEBUG` |
| The desktop shape | The Avalonia bench, end to end, against the production service |
| The service shape | Two containers in Docker: distinct fingerprints, mutual refusal, clone refused, configured `installId` surviving volume destruction |
| Issuance, delivery, key ring export | Run once, for real, on `test-client` |
| **Embedded key ring in a shipped product** | **Not yet.** The ring generator rests on its tests until WitSweep takes it |
| **A real product integration** | **Not yet.** WitSweep is next |
| Obfuscation — string encryption over the constant ring, anti-tamper | Not yet. `ENFORCEMENT.md` §11.7 sets the plan and ranks it honestly |
