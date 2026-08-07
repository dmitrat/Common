# OutWit.Common.Licensing

Offline software licensing for .NET. A compact signed token, a key ring scoped
per product line, tolerant machine binding, and validation that tells you *why*
rather than just *no*.

No BouncyCastle, no vendor service, no phone-home. A product that never sees the
internet works forever.

## Install

```bash
dotnet add package OutWit.Common.Licensing
```

## The licence

```
base64url(header) . base64url(payload) . base64url(signature)
```

One line — it survives an e-mail client, a text field, an environment variable
and a QR code.

**The signature covers the exact bytes of `header.payload` that the token
carries.** Verification never re-serialises anything, which removes the defining
failure of signing a serialiser's output: there, adding one field to a model
invalidates every licence ever issued, and the workaround lives forever. Here,
adding fields is free and unknown fields from a newer issuer pass through
untouched.

```jsonc
{
  "jti": "8f14e45f…", "iat": "2026-08-05T10:22:31Z",
  "product": "WitSweep",            // hard-checked
  "edition": "Enterprise",          // LABEL ONLY — never branch on this
  "appVer": ">=1.5.0 <2.0.0",
  "customer": { "id": "acme", "name": "ACME GmbH" },
  "nbf": "2026-08-05T00:00:00Z",
  "exp": "2027-08-05T00:00:00Z",    // ABSENT = unlimited
  "binding": { "kind": "machine", "threshold": 2, "factors": [ { "k": "…", "h": "…" } ] },
  "limits":   { "maxNodes": 50 },
  "features": [ "sso", "accounting" ],
  "supersedes": [ "3c1f…" ]
}
```

### `edition` is a label; `features` and `limits` are the contract

Branch on `HasFeature(key)` and `Limit(key)`. **Never on `edition`.**

The moment a product contains `if (edition == "Enterprise")`, the set of valid
editions becomes a fact compiled into a binary, and every new tier — an academic
price, an OEM bundle, a one-off deal — turns into a code change and a release.
With attributes it is a row in a table.

## Use it

```csharp
services.AddLicensing(options => options
    .ForProduct("WitSweep", ThisAssembly.Version)
    .WithKeyRing(LicenseKeyRing.FromJson(EmbeddedRingJson()))
    .WithBinding(new LicenseBindingProviderMachine())
    .WithStore(new LicenseStoreFile(licenceDirectory))
    .WithDemo(TimeSpan.FromDays(30), demo => demo.Limit("maxVariants", 8))
    .Declares(v => v
        .Feature("format.nas", "Nastran decks")
        .Limit("maxVariants", "Variants per sweep", @default: 64)));
```

```csharp
public sealed class RunViewModel(ILicenseService licensing, ISession session)
{
    private async Task RunAsync()
    {
        if (!licensing.State.CanRun)                    // may this run here?
            { ShowLicenceRequired(licensing.State.Describe()); return; }

        if (!await session.EnsureSignedInAsync())       // who are you? — a SEPARATE question
            { ShowSignInRequired(); return; }

        await session.SubmitAsync(BuildWork());
    }
}
```

**Nothing on `ILicenseService` accepts a user, a token or a `ClaimsPrincipal`,
and that omission is deliberate.** Whether a program may run on this machine and
who is signed into it are separate questions; an API that could be asked about a
user would eventually be asked, and the two would fuse.

## Validation answers with a reason

```csharp
Valid · Missing · Malformed · UnknownKeyId · SignatureInvalid · WrongProduct
WrongVersion · BindingMismatch · NotYetValid · Expired · ClockTampered
ExceedsKeyPolicy · Superseded
```

`Describe()` renders each into a sentence a person can act on —
*"Licence to ACME GmbH expired on 2027-08-05"*, not *"invalid"*. That is the
difference between a thirty-second support call and a day of e-mail.

## Binding tolerates drift

Hardware changes one component at a time. A single combined hash turns a
replaced network card into a dead licence, so a binding records factors
**individually** and demands *n of m*:

```csharp
new LicenseBindingProviderMachine()   // machine-id, primary-mac, machine-name
new LicenseBindingProviderTenant(tenantSlug, installId)
new LicenseBindingProviderComposite(…)
```

Bind a **containerised server to its deployment, never to hardware** — inside a
container the OS machine identity is not stable across a recreated container, so
a hardware-bound server licence dies on an ordinary redeploy.

A factor the host cannot produce simply does not count: neither a match nor a
violation.

## Keys are scoped, not global

```csharp
new LicenseKeyInfo {
    KeyId = "wsw-2026", Algorithm = LicenseAlgorithm.ES256,
    Products = ["WitSweep"],            // may sign nothing else
    Policy   = LicenseKeyPolicy.Commercial,
    PublicKeyPem = "-----BEGIN PUBLIC KEY-----…"
}
```

A product embeds only the ring of **its own product line**, so a leaked key for
one product cannot mint a licence for another, and recovery is mechanical:
retire the key, ship a build with a new `kid`, reissue that line.

The header's `alg` is only ever **checked** against what the ring registers for
the key — it never gets to choose. That closes the algorithm-substitution hole.

Supported: `ES256` (default), `ES384`, `ES512`, `RS256`, `PS256`. Keys are
standard PEM (SPKI / PKCS#8), so `openssl` can generate, inspect and rotate them.

## Fingerprints people can read

```
WSW-K3M9-7TQZ-B2XF-R8VN
```

Crockford Base32: no `I`, `L`, `O` or `U`, confusable pairs fold together on
input, and the last symbol is a check digit that catches **every** single-character
typo — before a wrong fingerprint becomes a wrong licence and a second support
cycle.

## The awkward parts, said plainly

- **An offline check is patchable.** Obfuscation raises the cost; it does not
  remove the risk. A licence makes non-compliance a decision somebody has to
  make on purpose — that is the job, and it is enough for business software.
- **Offline means no revocation.** The only lever is expiry, which is why the
  optional `checkIn` field exists for customers who *do* have a network, and why
  granting an unlimited term is a permanent decision.
- **The clock is the product's only time source**, so the store keeps a
  high-water mark and refuses to be wound back past it — with 24 hours of
  tolerance, because NTP corrections and resumed VM snapshots are not attacks.

## License

Licensed under the Apache License, Version 2.0. See `LICENSE`.

## Attribution (optional)

If you use OutWit.Common.Licensing in a product, a mention is appreciated (but
not required): "Powered by OutWit.Common.Licensing (https://ratner.io/)".

## Trademark / Project name

"OutWit" and the OutWit logo are used to identify the official project by Dmitry
Ratner. You may refer to the project name in a factual way (e.g., "built with
OutWit.Common.Licensing") or to indicate compatibility. You may not use the name
as the name of a fork or derived product in a way that implies it is the official
project, nor use the OutWit logo to promote forks or derived products without
permission.
