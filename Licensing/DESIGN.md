# OutWit Licensing — Design

> **Status: built (2026-08-10).** Design for `OutWit.Common.Licensing` and its
> integration into the OutWit product family. Layout follows the house
> convention of [`ARCHITECTURE.md`](../../WitCloud/ARCHITECTURE.md) and
> [`PLUGINS_GUIDE.md`](../Plugins/PLUGINS_GUIDE.md).
>
> This document is the format and the rules.
> [`ENFORCEMENT.md`](ENFORCEMENT.md) is how a product behaves when it reads a
> licence, and why. [`INTEGRATION.md`](INTEGRATION.md) is what to type.

---

## 1. What this covers

Wave 1 of OmnibusCloud is open: open server, open worker clients, open
initiators. Nothing is licensed. Wave 2 sells **on-prem WitCloud** and
**commercial desktop clients** (WitSweep is the reference client) to companies,
and needs an entitlement mechanism.

This document defines:

- the license document format and its cryptography;
- what a "fingerprint" is per product, and why it differs;
- the end-to-end flows (install → demo → request → issue → activate → renew);
- where enforcement hooks into WitCloud and WitSweep;
- who issues licenses and where that code lives;
- what is explicitly *not* protected, and why that is acceptable.

Out of scope: pricing, contract terms, the public crowd side of OmnibusCloud
(worker clients stay unlicensed by design — see §10.4).

---

## 2. Locked decisions

| Decision | Choice | Rationale |
|---|---|---|
| Metering | Flat per server + `edition`, **and** `maxNodes` | Node count is the natural unit for distributed compute; edition carries feature flags |
| Connectivity | **Fully offline. No online check of any kind.** | On-prem is sold on sovereignty; a CAE/defence customer who bought on-prem to be air-gapped will not accept a phone-home |
| Behaviour on expiry | **Degrade — refuse new work, finish current** | A hard stop at 3am gets the licensing ripped out of the product, not renewed |
| License scope | One document per product | Independent terms and renewal cadence for server vs client |
| **Identity vs entitlement** | **Two orthogonal axes; neither gates the other** | See §5 — this is the load-bearing rule of the whole design |
| **Desktop client scope** | **Per workstation (machine-locked), not per user** | One exe on one machine is used by several people signing into their own accounts; what is sold is the right to run the program here |
| **Term** | **Chosen by the admin at issue time, `unlimited` supported** | Not hardcoded anywhere. Absent `exp` means no expiry |
| Format | JWS-style compact token, ES256 (ECDSA P-256) | Canonical signing by construction; 86-char signature; in the BCL, no BouncyCastle |
| Build vs buy | Build as `OutWit.Common.Licensing` | Analysed in §3.4 |

---

## 3. Threat model and honest limits

### 3.1 Who we are defending against

The buyer is an organisation with procurement, legal, and a reputation. The
realistic failure modes are, in order of likelihood:

1. **Accidental over-use** — the pilot deployment keeps running after the
   contract lapses; a second instance gets stood up "just for testing"; node
   count creeps past what was bought.
2. **Quiet over-use** — the same, but noticed and ignored.
3. **Deliberate copying** — the license spreads to a sister company or a
   subsidiary that did not pay.
4. **Cracking** — someone patches the binary.

The mechanism must make (1) impossible, (2) and (3) *deliberate and visible*,
and simply accept (4).

### 3.2 What is not defended

- **Any offline check is patchable.** Obfuscation (WitCloud.Client already runs
  Eazfuscator) raises the cost; it does not eliminate the risk. Say so in the
  docs rather than pretending otherwise.
- **Offline means no revocation.** With no network call, a issued license cannot
  be withdrawn. The only lever is its expiry date. This drives §6.2 (shorter
  terms) and §14 (renewal must be cheap and routine).
- **A copied deployment volume clones the server identity.** Mitigated by
  contract, not by code. See §7.2.
- **Trial re-arming.** Deleting local state to restart a trial can be made
  annoying (§9) but not impossible offline.

### 3.3 What does the real work

The license is the *technical expression of a contract*, not a DRM system. The
commercial defence is that the valuable part of the product is a **service** —
updates, controller catalogue, support, the fleet, the portal — not the bits on
disk. The license file makes non-compliance a decision somebody has to make on
purpose. That is the whole job.

### 3.4 Why not an existing library or SaaS

| Option | Verdict |
|---|---|
| **`Standard.Licensing`** (OSS, Portable.Licensing successor) | Rejected. Pulls BouncyCastle (base OutWit packages are deliberately low-dependency), XML format, no `ModelBase`, no hardware binding, no revocation, no canonical-signing discipline. Solves the easy 20% (sign a blob) while dictating the file format; the hard 80% — cross-platform binding, clock resistance, storage, grace, DI, product wiring, key custody, issuing tooling — gets written anyway. |
| **SaaS (Cryptolens / Keygen.sh / LicenseSpring)** | Rejected. Recurring per-product cost, and an external hard dependency in the boot path of a product whose selling point is on-prem sovereignty. Also duplicates WitIdentity, which already holds customers and accounts. |
| **Build as `OutWit.Common.Licensing`** | **Chosen.** Every hard piece already exists in-house: `OutWit.Common.Platform` (cross-platform machine identity + standard directories), `ModelBase`, `OutWit.Common.Plugins`, and `OutWit.Cloud.Auth.TokenProtector` as the pattern for at-rest encryption on all three OSes. Crypto is entirely BCL. |

---

## 4. Topology — who issues, who verifies

```
        ┌──────────────────────────────────────────────────────────┐
        │  VENDOR SIDE (internet)                                  │
        │                                                          │
        │  licence.omnibuscloud.com  =  WitLicense                  │
        │    a standard OutWit service (§12) — same template as     │
        │    WitForms / WitAnalytics / WitIdentity                  │
        │                                                          │
        │    :7700 Kestrel   — WASM admin, public /checkin          │
        │    :7701 WitRPC    — admin channels                       │
        │                                                          │
        │      • the REGISTRY: customers, records, statuses,        │
        │        key vault, audit log, expiry watch                 │
        │      • SIGNS server-side — the private key never leaves   │
        │        the host, and by construction never reaches the    │
        │        browser: WASM receives only the finished token     │
        │      • delivery: save-to-disk in the browser, or one      │
        │        button to email it through IEmailTransport         │
        │                                                          │
        │    auth.omnibuscloud.com = WitIdentity  ← operators sign  │
        │      in here by OIDC; customers looked up over S2S.       │
        │      WitIdentity itself is NOT MODIFIED.                  │
        └──────────────────────────────────────────────────────────┘
                                   │
                    license text / .lic file / email
                                   │
                                   ▼
        ┌──────────────────────────────────────────────────────────┐
        │  CUSTOMER SIDE (may be fully air-gapped)                 │
        │                                                          │
        │  WitCloud server        ← verifies witcloud.lic (deployment) │
        │  WitIdentity (on-prem)  ← unchanged; knows nothing of it  │
        │  WitSweep workstations  ← each verifies its own           │
        │                            witsweep.lic, locally, offline │
        │  Worker clients         ← unlicensed by design            │
        │  Blender / 3ds Max addons ← unlicensed by design          │
        └──────────────────────────────────────────────────────────┘
```

### 4.1 Why a standalone service, not a WitIdentity plugin

Earlier drafts of this document put the vendor side inside WitIdentity as a
plugin, reasoning that WitIdentity "already has ~90% of what a licence admin
needs — customers, an admin UI, email, DB providers, S2S". Every item on that
list is true and every item is **a NuGet package, not a property of the host**:

| "Only WitIdentity has it" | Actually |
|---|---|
| Customers | Reachable over its S2S admin API — no code sharing needed |
| Blazor admin UI | `OutWit.Shared.Blazor.Shell` + `OutWit.Identity.Blazor` — packages |
| Email / messenger | `OutWit.Shared.Email.Provider.*` — plugins, usable in any host |
| DB providers | `OutWit.Common.Plugins` + the per-service provider pattern — template |
| Operator sign-in | Standard OIDC client registration, like every OutWit admin surface |

The overlap is **package-level, not host-level**. Co-hosting bought nothing that
a `PackageReference` does not, while costing a build coupling into the identity
server, a Dockerfile staging discipline, and a UI-delivery problem (a runtime
plugin cannot add pages to a pre-built WASM bundle).

**WitLicense is therefore a normal OutWit service**, cloned from the same product
template as WitForms and WitAnalytics (§12). That template is the ecosystem's
standard unit of work — host, contracts, DB providers, WASM admin, Docker, Caddy
— so most of the service arrives already written.

The "absent on-prem" property gets *stronger*, not weaker: it is no longer "the
module is not staged into the image" but **the service does not exist in the
customer's deployment at all**, and WitIdentity is not modified in any way.

Evolution path unchanged: self-service renewal and download can later be a Portal
surface over the same WitRPC contracts.

### 4.2 Key custody

If the licensing plugin runs on `auth.omnibuscloud.com`, then the private
signing key would sit on an internet-facing host. That key is the crown jewel:
compromise means the ability to mint unlimited licenses for every customer,
forever, unfixable without shipping a new build to everyone.

Private keys live in the WitLicense host's **vault**, envelope-encrypted under a
key-encryption-key held **outside** the database (env var / mounted secret), with
every signature written to the audit log. The admin UI is WASM, so it can never
hold a key even accidentally — it asks the host to sign and receives a finished
token (§12.3).

What actually bounds a compromise is **scoping**, not location. A product embeds
only the key ring of **its own product line** (§6.3.2), so two independent limits
apply to any forged token: it can only name products its key is scoped to, and it
can only grant what that key's policy permits. A leaked WitSweep key cannot mint
a WitCloud licence, and a leaked trial key cannot mint an Enterprise one.

Recovery from a compromised key is therefore bounded and mechanical: retire the
key, ship a build carrying a new `kid` for that line, and reissue that line's
licences — a worklist the registry can produce (§12.4).

`LicenseKeyInfo.Custody` remains an explicit field so an HSM, a KMS, or an
offline-signing mode is a later arm rather than a redesign. Only server custody
is implemented: a signing key on a laptop is not obviously safer than one in an
audited, backed-up, envelope-encrypted vault, and it would cost the entire
browser-based workflow.

---

## 5. Identity and entitlement are orthogonal

**This is the load-bearing rule of the design. Everything else follows from it.**

Two questions exist, they are answered by different systems, and **neither may
gate the other**:

| Question | Answered by | Scope |
|---|---|---|
| **"Who are you, and which fleet may you use?"** | **WitIdentity** — OIDC sign-in, api-keys, project/fleet membership | Per **user** |
| **"Which software may be run on this machine?"** | **The license document** — signed, offline, machine-locked | Per **machine** |

### 5.1 Why they must stay separate

The platform's free tier is genuinely free. Someone installs Blender with our
freely-distributed OmnibusCloud addon, signs in with their own account, and runs
renders on the fleet available to them. Nothing about that grants any right to
run WitSweep.

Conversely, one WitSweep installation on one workstation is used by several
engineers, each signing into **their own** account with **their own** fleet.
They are not sharing a licence between them — the workstation is licensed, and
whoever sits at it may use it.

So:

- **A cloud account never implies a right to run a commercial client.** Sign-in
  authorises fleet access, nothing more.
- **A client licence never implies cloud access.** It says only "this program may
  run on this machine".
- **A licence is never bound to a user.** No `sub`, no email, no seat-per-person.
  The `customer` block in the payload is a commercial record for the admin panel,
  not an authorisation subject.

### 5.2 What this rules out

A previous draft of this document proposed enforcing WitSweep through a
**server-side seat lease keyed on the signed-in user**. That is wrong, on both
sides:

- a user with a valid cloud account but no purchase would get WitSweep on any
  machine they touched;
- a single licensed workstation shared by three engineers in shifts would burn
  three seats, when one workstation was bought.

The reasoning behind it — that WitSweep is a thin client and therefore always has
a server nearby — is factually correct (`OutWit.Sweep.Cloud` carries only
`OutWit.Cloud.Auth` + `Bridge.Session` + CalculiX model DTOs; there is no solver
in the app, and the only offline capability is viewing and exporting prior
results). But "a server is reachable" is an argument about *where a check could
run*, not about *what is being sold*. What is sold is the right to run the
program on a workstation, and that is a machine-scoped, offline-verifiable fact.

**WitSweep therefore uses a local, machine-locked licence file. This is the
primary and, for stage 1–3, the only mechanism.**

### 5.3 Where floating fits — later, and still not per-user

Floating remains a legitimate *alternative purchase form*, as in every CAE tool
that sells node-locked and floating side by side. If it lands (§14, stage 4),
the counted unit is a **concurrently running instance**, not a user:

> "up to N copies of WitSweep running at any moment, on any machines"

That needs a reachable counter — the on-prem WitCloud server for an office
deployment, the public one otherwise — and a check-in keyed on the **installation
identity**, not on a signed-in principal. A licensed floating instance must be
able to check in *before and independently of* any user sign-in, or the
separation above is broken again.

### 5.4 What is never gated

The parts of WitSweep that work offline — open a deck, inspect it, view and
export prior results to CSV/XLSX/HTML — must never be held hostage. A customer
whose licence lapsed still owns their data and must be able to get it out. The
gated verb is **Run** — the productive action, and the one that consumes the
vendor's compute.

---

## 6. The license document

### 6.1 Format

```
witcloud.lic   =   base64url(header) "." base64url(payload) "." base64url(signature)
```

A JWS-style compact token: one line, no padding, safe in email, in a text field,
in an env var, in a QR code.

**The signature is computed over the exact bytes of the string
`base64url(header) "." base64url(payload)`** — the bytes the token itself
carries. Verification never re-serialises anything.

This single property kills the defining bug of the legacy `Norav.Common.Licensing`,
whose signature was taken over `JsonConvert.SerializeObject(...)` output. The
scar is visible in that codebase: adding one `bool` field invalidated every
issued license and left a permanent workaround —

```csharp
private const string RDP_TAIL = ",\"AllowRdp\":false";
...
if (!licenseData.CheckRsa(...) && !licenseData.Replace(RDP_TAIL, "").CheckRsa(...))
```

With byte-exact signing, adding fields is free and unknown fields from newer
issuers are carried through transparently.

### 6.2 Payload schema

```jsonc
{
  "jti":      "8f14e45f-ea8f-4b3d-9c1a-2b7d5e6a0c33",   // issuance id, quoted in support
  "iat":      "2026-08-05T10:22:31Z",
  "product":  "WitCloud",                  // hard-checked; "WitSweep" for the client
  "edition":  "Enterprise",                // LABEL ONLY — never branched on (§6.2.2)
  "appVer":   ">=1.5.0 <2.0.0",            // product version range this license covers
  "customer": {
    "id":     "acme-gmbh",
    "name":   "ACME GmbH",
    "contact":"it@acme.example"            // shown in the admin UI, not enforced
  },
  "nbf":      "2026-08-05T00:00:00Z",
  "exp":      "2027-08-05T00:00:00Z",      // ABSENT = unlimited, see below
  "binding":  {
    "kind":      "tenant",                 // none | tenant | machine | composite
    "threshold": 1,                        // N-of-M, see §7.3
    "factors":   [
      { "k": "tenant",    "h": "9f2b...c1" },
      { "k": "installId", "h": "41ae...77" }
    ]
  },
  "limits":   {
    "maxNodes":          50,
    "maxConcurrentJobs": 25
  },
  "features": ["accounting", "sso", "admin-api"],
  "supersedes": ["3c1f...", "…"],          // optional — jti's this document invalidates (§8.6a)
  "checkIn":  null,                        // optional — opt-in revocation (§8.6c); null = never contacts anything
  "notes":    "Renewal PO-2026-0417"       // free text, shown in the admin panel
}
```

Rules:

- `product`, `nbf`, `exp`, `binding` are **library-enforced invariants**, not
  user parameters. The legacy library put `ExpirationDate` in a caller-supplied
  `Parameters` object and then never checked it in `IsValid()` — every caller had
  to remember. That class of mistake is designed out.
- `appVer` uses a SemVer range so a major upgrade can require a new purchase
  without breaking the current install.
- Absent limit = unlimited. Absent feature = disabled.

### 6.2.1 Term is an issue-time decision

The term is **chosen by the admin when the licence is created** — a duration
(30 days, 1 year, 3 years) or an explicit end date. Nothing in the code assumes
a default term; the issuing UI and CLI both require the operator to state it.

**`exp` absent means unlimited.** A 100-year date would work in practice, but an
absent field is better: it displays as *"Unlimited"* rather than
*"expires 2126-08-05"*, it makes the commercial intent auditable in the record,
and it removes a comparison against a date nobody meant literally.

Two consequences to hold in mind when granting one:

1. **Offline means an unlimited licence can never be withdrawn.** There is no
   revocation channel (§3.2). Granting `unlimited` is a permanent decision.
2. **`appVer` is the safety valve.** The standard on-prem shape —
   *perpetual for the version you bought, maintenance for upgrades* — is
   expressed as `exp: absent` plus `appVer: ">=1.5.0 <2.0.0"`. The customer keeps
   what they paid for forever; a major upgrade needs a new document. This is the
   recommended form of "unlimited".

Renewal must still be routine (§8.3) for time-limited licences, which is why
issuance has to be cheap (§12).

### 6.2.2 `edition` is a label; `features` and `limits` are the contract

**No product may branch on `edition`.** It exists for humans — invoices, the
books, the support conversation, the line in the licence panel. The only inputs
to a runtime decision are `HasFeature(key)` and `Limit(key, fallback)`.

The reason is the universality rule. The moment a product contains
`if (edition == "Enterprise")`, the *set of valid editions* becomes a fact
compiled into a binary, and the licensing system stops being able to express
anything that set did not anticipate. Every new tier — Community, Education,
an OEM bundle, a one-off deal for a customer who needs exactly two Enterprise
features — becomes a code change and a release.

With attributes instead:

| Want | Cost |
|---|---|
| A new tier ("Education", 8 nodes, SSO but no accounting) | A row in the issuing catalogue. No code, no release |
| A one-off bundle for a single customer | Tick the features on the issue form. Not even a row |
| A capability split nobody predicted (WitSweep by input format: `format.inp` / `format.nas` / `integration.prepomax`) | Add keys to the product's declared vocabulary (§11.2) |

`ILicenseService` therefore exposes `State.EditionLabel` as a display string,
documented as non-semantic, while `HasFeature` / `Limit` are the ergonomic path.
The API makes the correct thing the easy thing.

**Consequence for planning: the edition list is deliberately undecided, and
decides nothing.** Nothing in stages 0–2 waits on it. The first sale can be
issued by ticking features directly; a named bundle gets created the second time
the same combination is sold. The catalogue then grows out of real deals instead
of speculation.

### 6.3 Cryptography

| Choice | Value | Why |
|---|---|---|
| Default algorithm | **ES256** — ECDSA P-256 + SHA-256 | In the BCL on every target TFM; no BouncyCastle. 64-byte signature → 86 base64url chars (RSA-2048 would be 344) |
| Algorithm | **Selected per key, carried in `alg`** | See §6.3.1 — not hardcoded |
| Key format | PEM — SPKI public, PKCS#8 private | Standard. Generated, inspected and rotated with `openssl`; storable in any secret manager. The legacy format (`RSAParameters` → Newtonsoft JSON → base64) was interoperable with nothing |
| Key id | `kid` in the header | Rotation, product-line scoping and the trial/commercial split (§6.3.2). The legacy tool held **one global key pair in its settings file** — no rotation, no scoping, one leak compromises everything ever issued |

Target frameworks: `net8.0;net9.0;net10.0`. **No `netstandard2.0`** — `ImportFromPem`
and friends need .NET 5+, and the newer Common packages (`Settings`, `Platform`,
`Logging.Query`) already draw the line there.

### 6.3.1 The algorithm is a property of the key, not of the library

`alg` lives in the token header and is read from the key's own metadata. The
verifier looks up `kid` → key → algorithm, and refuses a token whose `alg` does
not match what that key is registered for (this closes the classic JWS
algorithm-substitution hole — the header is never trusted to *choose* the
algorithm, only to be checked against the registry).

Supported set is an enum the library maps to BCL primitives:

| `alg` | Signature size | Notes |
|---|---|---|
| `ES256` / `ES384` / `ES512` | 64 / 96 / 132 bytes | Default. `ECDsa` in the BCL |
| `RS256` / `PS256` | 256 bytes | For a product line that must interoperate with existing RSA tooling |
| `ML-DSA-44` | ~2.4 KB | Post-quantum, available in .NET 10. Future-proofing only — the token stops being a one-liner, so not a default |

A new algorithm is one enum arm plus a mapping — no format change, because the
signature is just bytes over a canonical string (§6.1).

### 6.3.2 Keys are scoped to product lines

There is no single system key. A key is a record:

```csharp
public sealed class LicenseKeyInfo : ModelBase
{
    public string   Kid { get; init; }            // "owl-sweep-2026"
    public string   Algorithm { get; init; }      // "ES256"
    public string   PublicKeyPem { get; init; }
    public string[] Products { get; init; }       // ["WitSweep"] — this key may sign nothing else
    public KeyPolicy Policy { get; init; }        // Commercial | TrialOnly
    public DateTime? RetiredUtc { get; init; }    // still verifies, no longer signs
}
```

Verification enforces the scope: a token whose `product` is not in its key's
`Products` fails with `ExceedsKeyPolicy`, **even if the signature is perfect.**

Consequences that matter:

- **A product embeds only the keys of its own line.** WitSweep's build carries
  the WitSweep key ring; WitCloud's carries its own. A compromised WitSweep key
  cannot mint a single WitCloud licence.
- **Blast radius is a product line, not the company.** Recovery is: retire the
  key, issue a build with a new `kid` for that line, reissue that line's
  licences. Everything else is untouched.
- **Trial keys stay trial keys** (§4.2) — `Policy = TrialOnly` caps what they may
  grant regardless of what the payload claims.
- **Different lines may use different algorithms** — an OEM partner who requires
  RSA gets an RS256 key without affecting anyone else.

Retiring a key keeps it in verifiers (already-issued licences must keep working)
while removing it from signers. Marking a key **compromised** is different: the
registry can list every licence it ever signed, which is the reissue worklist.

### 6.4 Validation result

Validation returns a **reason**, never a bare `bool`:

```csharp
public enum LicenseStatus
{
    Valid,
    Missing,           // no license present at all → demo mode
    Malformed,         // not a token / bad base64 / bad JSON
    UnknownKeyId,      // signed with a kid this build does not trust
    SignatureInvalid,
    WrongProduct,
    WrongVersion,      // appVer range excludes the running build
    NotYetValid,       // nbf in the future
    Expired,
    BindingMismatch,   // fingerprint does not satisfy the N-of-M threshold
    ClockTampered,     // system clock rolled back past the high-water mark
    ExceedsKeyPolicy   // trial key trying to grant commercial entitlements
}
```

Every failure path also returns the parsed payload when the token is
structurally sound, so the UI can say *"Enterprise license for ACME GmbH expired
on 2027-08-05"* instead of *"invalid license"*. This is the difference between a
30-second support call and a day of email.

---

## 7. Identity and binding

### 7.1 A fingerprint is not the same thing per product

| Product | Binds to | Factors | Threshold |
|---|---|---|---|
| **WitCloud server** | The *deployment* | `tenant` (slug from `tenant.json`), `installId` (128-bit random, generated at first start) | 1 of 2 |
| **WitSweep / desktop** | The *workstation* | `machineId`, `osInstall`, `primaryMac` | 2 of 3 |
| Anything cloud-hosted by us | Nothing | — | `kind: none` |

Note what is **absent** from every row: the user. Binding factors are properties
of a deployment or a machine, never of a principal (§5). A desktop licence
travels with the workstation and serves whoever sits at it.

### 7.2 Why the server must NOT bind to hardware

WitCloud ships as **one Docker image**. Inside a container, `/etc/machine-id` is
not stable across recreation, and `MachineIdentityProvider` falls back to a
generated file that an ephemeral container regenerates every time. A
machine-bound server license would die on every
`docker compose up --force-recreate` — the classic node-locking trap in
containerised products.

So the server binds to the **deployment identity**:

- `tenant` — the slug the installer already writes into `tenant.json`. Stable by
  definition, human-meaningful, and the same string that appears in the contract.
- `installId` — 128 random bits written once to a **mounted volume**
  (`/app/license/install-id`), so it survives container recreation but not a
  fresh deployment.

At **1-of-2**, the license survives a lost volume (tenant still matches) *and* a
tenant rename (installId still matches), while a copy dropped into a different
customer's deployment satisfies neither.

Honest limitation: copying the volume *and* the tenant slug clones the identity.
That is a deliberate act covered by contract, not by code.

### 7.3 Fuzzy binding — N of M

The legacy library hashed all hardware factors into **one** string and compared
it whole. A RAM upgrade, a re-imaged disk, a replaced NIC → dead license → support
ticket. And its disk factor read `Assembly.GetExecutingAssembly().Location`,
which is **empty under single-file publish** — meaning it silently degraded to a
different fingerprint on exactly the deployment shape WitSweep (MSI) and the
worker client (Parcel) use.

Instead: the license carries a hash **per factor**, and validation passes when at
least `threshold` factors match. Hardware drifts one component at a time; a
2-of-3 machine binding absorbs that and still fails on a genuinely different
machine.

**Required upstream change:** `OutWit.Common.Platform` today exposes only a
single combined `IMachineIdentityProvider.GetMachineIdentityAsync()`. N-of-M
needs the individual factors, so Platform gains an
`IMachineFactorsProvider → IReadOnlyList<MachineFactor(Key, Value)>` alongside
it. This is a prerequisite of the licensing work, not part of it.

### 7.4 Display form

A SHA-256 hex string is 64 characters — unusable on the phone and error-prone in
email. The fingerprint is shown as **Crockford Base32 with a check symbol**:

```
WSW-K3M9-7TQZ-B2XF-R8VN-C
│   └──── 80 bits, 4 groups of 4 ────┘ └ check
└ product prefix (WSW / WCL)
```

Crockford Base32 excludes `I`, `L`, `O`, `U` and treats confusable characters as
equivalent on input, so `0`/`O` and `1`/`I` typos self-correct; the check symbol
catches the rest **before** a wrong fingerprint becomes a wrong license and a
support cycle. 80 bits is collision-free at any realistic customer count.

---

## 8. Flows

### 8.1 F1 — Server: install → demo → licensed

```
 operator                     WitCloud server                    vendor
    │                               │                               │
    │ deploy (installer zip /       │                               │
    │ docker compose up)            │                               │
    ├──────────────────────────────►│                               │
    │                               │ first start:                  │
    │                               │  • generate installId → volume│
    │                               │  • self-issue DEMO license    │
    │                               │    (§9), binding=tenant       │
    │                               │  • start NORMALLY, degraded   │
    │                               │                               │
    │ opens admin UI → Licensing    │                               │
    │◄──────────────────────────────┤                               │
    │   "Demo — 27 days left,       │                               │
    │    max 2 nodes"               │                               │
    │   [ Copy installation request ]                               │
    │                               │                               │
    │  request blob (§8.4)          │                               │
    ├───────────────────────────────────── email / portal ─────────►│
    │                               │                               │ admin UI imports
    │                               │                               │ the blob → record
    │                               │                               │ in the registry;
    │                               │                               │ operator picks term
    │                               │                               │ + edition → issue
    │  license token (one line)     │                               │
    │◄───────────────────────────────────── email ──────────────────┤
    │                               │                               │
    │ paste into admin UI           │                               │
    │  — or —                       │                               │
    │ set Licensing__License env    │                               │
    │  — or —                       │                               │
    │ drop witcloud.lic into        │                               │
    │ the license volume            │                               │
    ├──────────────────────────────►│ validate → persist → apply    │
    │                               │ (no restart required)         │
    │◄──────────────────────────────┤ "Enterprise · ACME GmbH ·     │
    │                               │  50 nodes · until 2027-08-05" │
```

Notes:

- **The server always starts.** Demo is a real license (§9), not a special code
  path, so there is exactly one path through the code.
- Three delivery mechanisms because three situations: paste (interactive),
  env var `Licensing__License` (Docker — matches the ecosystem's
  `Section__Key` convention), file drop (installer / config management).
- Applying a license **must not require a restart.** `ILicenseService` re-reads
  and re-validates on demand; the enforcement points read current state.

### 8.2 F2 — Workstation: first launch → demo → licensed

Entirely local. **No server participates in this flow**, and no user needs to be
signed in — the licence answers "may this program run on this machine", which is
knowable offline (§5).

```
 whoever sits at the machine        WitSweep                      vendor
        │                              │                             │
        │ launch                       │                             │
        ├─────────────────────────────►│ no licence → DEMO (§9)      │
        │◄─────────────────────────────┤ app opens; "Demo, 30 days"  │
        │                              │                             │
        │ open deck / view / export    │ ← ALWAYS FREE (§5.4)        │
        │                              │                             │
        │ Settings → Licence           │                             │
        │◄─────────────────────────────┤ WSW-K3M9-7TQZ-B2XF-R8VN-C   │
        │                              │ [ Copy request ] [ Save … ] │
        │                              │                             │
        │  request blob (§8.4)         │                             │
        ├──────────────── email / portal ───────────────────────────►│ admin picks
        │                              │                             │ term + edition,
        │                              │                             │ owner signs
        │                              │                             │ offline
        │  licence token (one line)    │                             │
        │◄──────────────── email ─────────────────────────────────────┤
        │                              │                             │
        │ paste into Settings          │                             │
        │  — or drop witsweep.lic      │                             │
        ├─────────────────────────────►│ validate → persist → apply  │
        │◄─────────────────────────────┤ "Licensed · ACME GmbH ·     │
        │                              │  until 2027-08-05"          │
        │                              │                             │
        │ press Run                    │ licence OK → proceed        │
        ├─────────────────────────────►│ sign in (OIDC/PKCE) if not  │
        │                              │ already — SEPARATE axis:    │
        │                              │ decides WHICH FLEET, never  │
        │                              │ whether the app may run     │
        │                              ├────────────────────────────►│ submit sweep
```

Notes:

- **The licence check never consults the account, and sign-in never consults the
  licence.** A colleague sitting down at the same workstation signs into their
  own account and works against their own fleet, under the same workstation
  licence.
- The same workstation may hold licences for several products
  (`witsweep.lic`, a future `witX.lic`); each is checked by its own `product`
  field.
- A freely-distributed initiator (the Blender / 3ds Max addons) on the same
  machine is unaffected — it carries no licensing code at all (§10.4).

### 8.3 F3 — Renewal

The only lever an offline license has is its expiry, so renewal must be
frictionless:

- **Warn early and prominently** — banner from 30 days out, escalating; the exact
  date and the `jti` are always one click away.
- **Overlap is supported.** A new license can be installed *before* the old one
  expires. The store keeps **multiple** license documents and selects the best
  currently-valid one (highest entitlement among those where
  `nbf ≤ now < exp`). This removes the whole class of "renewed on the right day
  but had a two-hour outage" incidents.
- Renewal reuses the identical delivery mechanisms — no separate flow.

### 8.4 The installation request — making "somehow sent to the admin" concrete

The weak point in a naive fingerprint flow is the hand-off. The app produces a
**request blob** (copy to clipboard, or save as `<product>-<fingerprint>.owlreq`):

```jsonc
{
  "v": 1,
  "product":     "WitCloud",
  "productVer":  "1.5.12",
  "fingerprint": "WCL-K3M9-7TQZ-B2XF-R8VN-C",
  "factors":     [ { "k": "tenant", "h": "9f2b…" }, { "k": "installId", "h": "41ae…" } ],
  "host":        { "os": "Linux 6.8 (container)", "name": "omnibus-prod-1" },
  "contact":     "it@acme.example",
  "requested":   { "edition": "Enterprise", "maxNodes": 50 }
}
```

It carries the **factor hashes**, not just the display code, so the admin never
retypes anything — paste the blob into the admin UI and the binding block is
filled in. The display code exists for the phone call, the blob for the actual
work.

### 8.5 F4 — Assisted online request (phase 3, optional)

For customers who *do* have internet, the request can be POSTed to the vendor's
portal and the license returned automatically. This is a **convenience layer over
the same artifacts** — same request blob, same token, same offline verification.
A licence issued without §8.6's opt-in check-in never contacts anything: a
product that never sees the internet is fully functional forever.

### 8.6 Transfer and revocation — what "Disabled" actually means

The registry (§12) marks licences `Active | Disabled | Superseded | Expired`.
It is worth being exact about what each does, because the word "revoke" promises
more than an offline system can deliver.

**Marking a licence `Disabled` in the registry is a commercial and audit fact,
not a kill switch.** The token already on the customer's disk keeps verifying
until it expires — there is nothing to check against, by design (§3.2). Anyone
who claims otherwise about an offline scheme is mistaken.

Three mechanisms give the state real teeth, in increasing strength:

**(a) Supersession — free, always on.** A newly issued licence may carry
`supersedes: ["<jti>", …]`. The store refuses any listed `jti` it finds locally,
even if that token is otherwise valid. This fully covers the cases where both
documents reach the same machine — renewal, edition change, a corrected reissue.
It does nothing for a token sitting on a machine that never sees the successor.

**(b) Term length — the real lever.** With no revocation channel, a licence's
maximum lifetime *is* its blast radius. This is the concrete reason §6.2.1 makes
`Unlimited` a deliberate, warned decision rather than a convenient default.

**(c) Opt-in check-in — real revocation, per licence, admin's choice.** The
payload may carry:

```jsonc
"checkIn": { "url": "https://licence.omnibuscloud.com", "everyDays": 7, "graceDays": 30 }
```

When present, the product periodically confirms the licence is still `Active` in
the registry, and enters a grace window when it cannot reach it — expiring only
after `graceDays` of continuous failure, so an outage or a holiday never kills a
production cluster. When **absent — the default — nothing is ever contacted.**

The admin chooses this at issue time, per licence, exactly like the term:

| Customer | `checkIn` | Result |
|---|---|---|
| Air-gapped on-prem | absent | Fully offline forever. No revocation, ever |
| Connected on-prem | present, generous grace | Genuine revocation; survives outages |
| Desktop seat on the public cloud | present | Genuine revocation |

This keeps the locked decision from §2 intact — *offline must remain possible* —
while giving revocation to every customer who does not need air-gap. It is one
optional field, not a second architecture.

#### The machine-transfer flow

The common real case, and now a designed flow rather than an afterthought:

```
 customer                       WitLicense admin UI              registry
    │                                │                            │
    │ "laptop replaced, need a new   │                            │
    │  licence" + new request blob   │                            │
    ├───────────────────────────────►│ finds old record by        │
    │                                │ fingerprint or customer    │
    │                                ├───────────────────────────►│
    │                                │◄───────────────────────────┤ old record
    │                                │ [ Transfer ]               │
    │                                │  • old → Disabled          │
    │                                │    (reason: Transferred)   │
    │                                │  • new licence issued for  │
    │                                │    the new fingerprint,    │
    │                                │    same term remaining,    │
    │                                │    supersedes: [old jti]   │
    │                                │  • both linked in history  │
    │◄───────────────────────────────┤ new token                  │
```

What this buys, honestly: a clean audit trail, a correct entitlement count, and
— if the old machine ever meets the new token — refusal by supersession. What it
does not buy: killing the old file on a machine that is never seen again. That
residual is bounded by the remaining term, which is the argument for terms in the
first place.

**Transfer policy is an open question** (§15) — free and unlimited, capped per
year, or requiring a reason — because the admin UI has to enforce whatever is
decided.

---

## 9. Demo mode

Demo is **a real license**, self-issued at first run and signed by nothing —
stored with an explicit `DemoLicense` marker and validated by the same code path
with the signature check replaced by a local-issue check.

Rationale: one code path. A separate `if (noLicense) { ... }` branch is where
licensing bugs live, and it is also where "delete the file to get free
functionality" lives.

| | Server demo | Workstation demo |
|---|---|---|
| Term | 30 days (product default, set in `AddLicensing`) | 30 days |
| Binding | `tenant` — same as a real server licence | `machine` — same as a real workstation licence |
| Limits | `maxNodes: 2`, `maxConcurrentJobs: 2` | Variant cap per sweep |
| Features | Core only — no accounting, SSO, admin-api | Core only |
| Visibility | Persistent banner in admin UI, `/health` field | Persistent banner, Settings panel |

The demo term is the one term the product decides for itself, because there is no
admin in the loop. Every *issued* licence takes its term from the admin (§6.2.1).

**Re-arm resistance** (best effort, honestly documented): the first-run timestamp
is recorded outside the app data directory (per-user config location from
`IStandardDirectoryProvider`), and the clock guard (§10.5) prevents winding time
back. Deleting *everything* restarts the demo. Offline, this cannot be fully
prevented; a 30-day demo is not worth more engineering than that.

---

## 10. Enforcement points

### 10.1 WitCloud server

| Limit / feature | Where | Behaviour when exceeded |
|---|---|---|
| `maxNodes` | `WitNodesManager` / `ClientPoolManager` at registration | Refuse the *new* node with an explicit reason; existing nodes untouched |
| `maxConcurrentJobs` | `ProcessingSchedulerService` | Queue rather than reject |
| `features[]` | Channel guards, beside the existing `EnsureAdmin()` | `Result.Unauthorized()` with a licensing reason |
| Expiry | Job intake | **Refuse new jobs; let running jobs finish.** Never a hard stop |
| State | `/health` + admin UI panel | Always visible, never silent |

### 10.2 WitSweep

| Gate | Behaviour |
|---|---|
| Launch, open deck, edit parameters, view results, export CSV/XLSX/HTML/report | **Never gated** (§5.4) |
| Run sweep | Requires a valid workstation licence, or demo within its caps. **Checked locally, offline, without reference to the signed-in account** |
| Sign-in / fleet selection | Governed by WitIdentity alone. **Never consults the licence** |
| State | Settings → Licence: edition, customer, term, fingerprint, paste field, request export |

The two checks meet only in the sense that both must pass to submit a sweep: the
licence says the program may run here, the account says which fleet it may run
on. Neither can substitute for the other, and a failure of one must never be
reported as a failure of the other — *"no licence for this workstation"* and
*"you are not signed in"* are different messages with different fixes.

### 10.3 Interaction with the existing accounting subsystem

WitCloud already has `AccountingQueryService` and
`AssignmentAccountingCaptureService`. Licensing does **not** duplicate them — it
sets *caps*, accounting measures *usage*. Where a limit needs a live count
(registered nodes, concurrent jobs), it reads the existing managers rather than
maintaining a parallel tally.

### 10.4 What is deliberately never licensed

Two whole product categories carry **no licensing code at all**, and this is a
standing rule, not an oversight:

- **Worker clients** — the open crowd side of wave 1. A provider donating spare
  cycles will not accept a licence check, and frictionless onboarding is the
  entire growth mechanism.
- **Initiators and addons** — the Blender and 3ds Max plugins, the SDK, anything
  built on the public `OutWit.Cloud.Contracts` / `OutWit.Cloud.SDK` surface.
  These are distributed freely on purpose: anyone with an account may submit work
  to their own fleet through them. That is the free tier, and it is precisely
  what makes §5's separation necessary — having one of these working says nothing
  about the right to run a commercial client.

Stated explicitly so nobody adds a check later "for consistency".

### 10.5 Clock guard

With no online check, a rolled-back clock is the primary bypass. The store keeps
a monotonic high-water mark of the greatest UTC seen, updated on every
validation. A system clock earlier than the mark by more than a tolerance
(default 24h, for legitimate NTP corrections and timezone-confused VMs) yields
`ClockTampered`, which degrades exactly like `Expired`.

---

## 11. Package layout

```
Licensing/
  DESIGN.md                                   ← this document
  OutWit.Common.Licensing/                    net8.0;net9.0;net10.0 · Apache-2.0 · published
    Abstract/LicensePayload.cs                : ModelBase
    Abstract/LicenseBinding.cs                : ModelBase
    Abstract/LicenseLimits.cs                 : ModelBase
    Interfaces/ILicenseService.cs
    Interfaces/ILicenseStore.cs
    Interfaces/ILicenseBindingProvider.cs
    Interfaces/ILicenseKeyRing.cs
    Bindings/LicenseBindingProviderNone.cs
    Bindings/LicenseBindingProviderMachine.cs
    Bindings/LicenseBindingProviderTenant.cs
    Bindings/LicenseBindingProviderComposite.cs
    Crypto/LicenseSigner.cs                   ES256, PEM
    Crypto/LicenseKeyRing.cs                  kid → public key, policy per key
    Crypto/FingerprintCodec.cs                Crockford Base32 + check symbol
    Validation/LicenseValidator.cs
    Validation/LicenseValidationResult.cs
    Validation/LicenseStatus.cs
    Storage/LicenseStoreFile.cs               multi-document, best-valid selection
    Storage/ClockGuard.cs
    Demo/DemoLicenseFactory.cs
    Requests/LicenseRequest.cs                the .owlreq blob
    ServiceCollectionExtensions.cs            AddLicensing(o => …)
    README.md
  OutWit.Common.Licensing.Tests/
  Samples/
    OutWit.Common.Licensing.Samples.Avalonia/ dev-only round-trip harness (§14.3)
```

`LicenseSigner` can both sign and verify — they are the same BCL primitive, and
hiding a twenty-line ECDSA call would buy nothing (Kerckhoffs). What is private
is the **key vault and the books**, and those live in `WitLicense` (§12), not
here.

Dependencies: `OutWit.Common`, `OutWit.Common.Platform`,
`Microsoft.Extensions.DependencyInjection.Abstractions`. **No** BouncyCastle,
Newtonsoft, `System.Management`, or WMI.

### 11.1 Consumer API

```csharp
services.AddLicensing(options => options
    .ForProduct("WitCloud", typeof(Startup).Assembly.GetName().Version!)
    .WithKeyRing(LicenseKeyRing.Embedded())        // both public keys + policy
    .WithBinding(LicenseBinding.Tenant(tenantSlug))
    .WithDemo(TimeSpan.FromDays(30), demo => demo.MaxNodes(2))
    .WithStore(LicenseStore.Default()));           // IStandardDirectoryProvider

// anywhere
public sealed class NodeRegistrationHandler(ILicenseService licensing)
{
    public Result Register(NodeInfo node)
    {
        if (licensing.State.Status is not LicenseStatus.Valid)
            return Result.Rejected(licensing.State.Describe());

        if (m_nodes.Count >= licensing.Limit("maxNodes", int.MaxValue))
            return Result.Rejected("Node limit reached for this license.");
        ...
    }
}
```

The workstation side, showing the separation of §5 in code — the licence check
takes no principal, and the sign-in check takes no licence:

```csharp
services.AddLicensing(options => options
    .ForProduct("WitSweep", ThisAssembly.Version)
    .WithKeyRing(LicenseKeyRing.Embedded())
    .WithBinding(LicenseBinding.Machine(threshold: 2))   // machineId / osInstall / primaryMac
    .WithDemo(TimeSpan.FromDays(30), demo => demo.MaxVariantsPerSweep(8))
    .WithStore(LicenseStore.Default()));

// RunViewModel — both gates, independent, distinct messages
private async Task RunAsync()
{
    if (!Licensing.State.CanRun)                       // machine entitlement
        { ShowLicenceRequired(Licensing.State.Describe()); return; }

    if (!await Session.EnsureSignedInAsync())          // identity — WHICH fleet
        { ShowSignInRequired(); return; }

    await Session.SubmitAsync(BuildSweep());
}
```

`ILicenseService` surface: `State` (status + payload + human description +
`CanRun`), `HasFeature(key)`, `Limit(key, fallback)`, `Fingerprint`,
`CreateRequest()`, `Install(token)`, `Reload()`. Nothing on it accepts a user,
a token, or a `ClaimsPrincipal` — by design, so the separation cannot be
accidentally violated by a later change.

### 11.2 Declared vocabulary and unknown-key reporting

`AddLicensing` also **declares what the product understands** — the feature and
limit keys it will ever check:

```csharp
.Declares(v => v
    .Feature("sso",        "Single sign-on")
    .Feature("accounting", "Usage accounting")
    .Limit  ("maxNodes",   "Worker nodes", @default: 4))
```

Two things fall out of it, and both matter:

1. **`State.UnrecognisedKeys`** — any feature or limit the licence grants that
   the product does not know. Surfaced in the licence panel and as a log line
   (*"licence grants unknown feature 'ssoo'"*). While the issuing catalogue is
   maintained by hand (WitLicense §6), this is what turns a silent typo into a
   visible one at first install, instead of a support ticket three weeks later.
2. **The descriptor export** — the same declaration serialises to
   `<product>.product.json`, which the registry imports so the issue form offers
   exactly the vocabulary the product implements. That closes the drift at the
   root, and it is why the declaration belongs in stage 1 even though the import
   side arrives later.

---

## 12. The vendor side — WitLicense, a standard OutWit service

> **The service has its own specification:**
> [`WitLicense/DESIGN.md`](../../WitLicense/DESIGN.md) — capabilities by tier,
> data model, catalogue, key vault, build order. This section is the summary and
> the parts that constrain the *format*; that document is the detail.

Everything vendor-facing lives in its own **private repository, `WitLicense`**.
None of it belongs in `Common`, which is public: the public package is the
*format and the verifier*, the private repo is the *factory and the books*.

It is built from the **OutWit product template** — the same one WitForms and
WitAnalytics were cloned from, which WitAnalytics's `DESIGN.md` names explicitly
("WitIdentity OIDC + Blazor WASM admin + WitRPC + plugin providers +
docker-compose/Caddy"). Following it is not ceremony: it is why most of this
service arrives already written.

### 12.1 Solution layout

Repo / image `WitLicense`, namespace root `OutWit.License`, `net10.0`
throughout, ports **7700** (Kestrel: WASM admin + public `/checkin`) and
**7701** (WitRPC), continuing the 7500 / 7600 series.

| Project | Role | Blueprint |
|---|---|---|
| `OutWit.License` | ASP.NET Core host: WitRPC admin channels, issuance + signing service, key vault, public `/checkin`, expiry watch, hosts the WASM admin | `OutWit.Analytics` |
| `OutWit.License.UI` | Blazor WASM admin — dashboard, issue, renew, transfer, keys, inspector (MudBlazor + MVVM + Identity shell) | `OutWit.Analytics.UI` |
| `OutWit.License.Contracts` | MemoryPack DTOs (`ModelBase`, `Is`/`Clone`) | `OutWit.Analytics.Contracts` |
| `OutWit.License.Interfaces` | WitRPC channel interfaces | `OutWit.Analytics.Interfaces` |
| `OutWit.License.Data` | EF entity POCOs | `OutWit.Analytics.Data` |
| `OutWit.License.Database` | `WitPluginLoader<IDatabaseProviderPlugin>` wrapper (`AddDatabase`) | same |
| `OutWit.License.Database.Abstractions` | Provider contract + neutral `WitLicenseDbContext` | same |
| `OutWit.License.Database.WitDatabase` | Embedded provider — **the production default here**: a few hundred rows a year does not want a Postgres | same |
| `OutWit.License.Database.PostgreSql` | Second provider, for parity and because the template's migration discipline wants two | same |
| `OutWit.License.Tests` | NUnit 4, mirrors host structure | same |
| `OutWit.License.Fingerprint` | The customer-facing desktop utility (§12.4) — outside the template, Avalonia | `Norav.Tools.Fingerprint` |

**Deliberately deferred**, exactly as WitAnalytics defers it: the Installer family.
We deploy one instance ourselves; installers matter only for distributable
products.

Ecosystem dependencies arrive as **NuGet packages, never `ProjectReference`
across repos** — `OutWit.Common.*` (including the new `.Licensing`),
`OutWit.Shared.*`, `OutWit.Communication.*`, `OutWit.Database.*`,
`OutWit.Identity.Blazor` / `.Profile` / `.Contracts.Shared`.

### 12.2 What the template supplies, and what is actually new

| Comes for free | Genuinely new work |
|---|---|
| Host wiring, Kestrel + WitRPC server, DI, config layering | The key vault — CRUD, envelope encryption, scoping, custody |
| DB provider plugin model + both providers + migration discipline | Issuance — build payload, sign, record, supersede |
| Operator sign-in (OIDC against WitIdentity), admin guards | The registry — statuses, transfer, search by fingerprint |
| Blazor shell, M3 theme, MVVM base, MudBlazor | The dashboard and issue form |
| Email / messenger / logging provider plugins | Expiry watch + notifications |
| Dockerfile, compose profiles, Caddy fragment, deploy runbook | The public `/checkin` endpoint (§8.6c) |

The new part is on the order of a few thousand lines. The rest is a clone.

### 12.3 Signing stays server-side by construction

This falls out of the template rather than needing a design decision: the admin
UI is **WASM**, so it can only ask the host to do things over WitRPC. Private
keys live in the host's vault, envelope-encrypted under a key-encryption-key held
**outside** the database (env var / mounted secret). The browser receives only
the finished token string.

Delivery, from the same screen:

- **Save** — the token comes back over the channel, JS interop writes
  `witcloud-acme-2026.lic` to the operator's downloads;
- **Send** — one button, the *host* emails it through the configured
  `IEmailTransport`, and the record notes when and to whom.

Key custody stays an explicit field on each key (§6.3.2) so a future HSM/KMS or
offline-signing mode is an added arm, not a redesign — but **only server custody
is implemented.** A private key on a laptop is not obviously safer than one in an
audited, backed-up, envelope-encrypted vault, and it would cost the whole
browser-based workflow.

### 12.4 What the admin UI does

The features that already existed in `Norav.Tools.LicenseGenerator` (features
picker, expiry, system lock, fingerprint import, key generation) plus the ones
its single-key, no-registry design could not have:

**Issuing**
- Import a request blob or `.fpt`-style fingerprint → binding prefilled, nothing
  retyped.
- Term is **required**: duration preset, explicit date, or *Unlimited* — with an
  inline warning that offline licences cannot be recalled, and the recommended
  alternative (unlimited + `appVer` ceiling).
- Edition, limits, features, `checkIn` policy (§8.6c), signing key.
- **Preview before signing** — render exactly what the customer will receive.
- **Deliver** — copy, save, or email through the host's `IEmailTransport`.

**The books**
- Dashboard of everything issued, **colour-coded by time remaining** — green /
  amber (<60 days) / red (<14 days) / grey (expired), plus a filter for
  `Unlimited`, which never ages and therefore never surfaces on its own.
- **Search by fingerprint** — the support path: a customer reads their code over
  the phone, the record appears.
- Customer view: every licence for one customer, across products.
- **One-click renew** — clone the record, shift the term, `supersedes` the old
  one automatically.
- **Transfer** (§8.6) — disable the old, issue for the new fingerprint with the
  remaining term, link both in history.
- **Disable** with a reason (`Transferred | Refunded | Breach | Superseded`), and
  an honest label in the UI that this is a bookkeeping state unless the licence
  carries `checkIn`.
- Audit log — who did what, when, why. Matters as soon as there is more than one
  operator.

**Keys**
- Create, scope to products, choose algorithm and custody, retire.
- Mark **compromised** → the registry lists every licence that key ever signed;
  that list *is* the reissue worklist.

**Notifications**
- Scheduled expiry warnings to the vendor, and optionally to the customer,
  through the host's existing `IEmailTransport` / `IMessengerTransport` (the
  Telegram provider already exists in `OutWit.Shared.Messenger.Provider.Telegram`).

**Support**
- Paste any token → decoded, validated against the key ring, verdict shown. No
  key needed for decoding. Half of all licence tickets end here.

### 12.5 The customer-facing fingerprint utility

`OutWit.License.Fingerprint` — a tiny Avalonia app, freely downloadable, the
direct descendant of `Norav.Tools.Fingerprint`. It shows the display code and
exports a request blob.

Products show their own fingerprint in-app, which is always preferable. This
exists for the cases where that is impossible: the product is not installed yet,
or it will not start. It reveals nothing — it only reads machine factors — so it
needs no protection.

### 12.6 Not responsibilities

The registry never enforces anything, and is never contacted by a customer
deployment — except by licences that explicitly opted into `checkIn` (§8.6c),
which reach a single public read-only endpoint, not the admin surface.

---

## 13. Delivery and storage on the customer side

| Mechanism | Primary use |
|---|---|
| Paste the token into the admin UI / Settings panel | Interactive, both products |
| `Licensing__License` env var | Docker / compose — matches the ecosystem's `Section__Key` convention |
| `<licenseDir>/*.lic` file drop | Installer, config management, air-gapped transfer by USB |
| Installer wizard field | First-time server deploy, alongside `tenant.json` |

Storage location comes from `OutWit.Common.Platform.IStandardDirectoryProvider`
(per-OS config directory) for clients, and a mounted volume for the server. The
license itself is **not secret** — it is signed, not encrypted, and readable by
design so an operator can inspect what they were granted. Only the sidecar state
(clock high-water mark, first-run marker) is written with restrictive permissions.

---

## 14. Staged plan

| Stage | Content | Unblocks |
|---|---|---|
| **0** | `OutWit.Common.Platform`: add `IMachineFactorsProvider` (§7.3) | Prerequisite |
| **1** | `OutWit.Common.Licensing` core: token format, pluggable `alg` + PEM + scoped key ring, payload, N-of-M binding, validation with reasons, file store with multi-document selection and supersession, clock guard, demo factory, `AddLicensing` + declared vocabulary + unknown-key reporting (§11.2), fingerprint codec, request blob. Full test suite | The whole mechanism exists |
| **1.5** | **Sample app** — Avalonia harness closing the full loop (§14.3) on Windows / Linux / macOS | The design is proven before a shipping product is touched |
| **2** | The `WitLicense` service (§12), cloned from the WitForms / WitAnalytics template — registry, key vault, WASM admin, issuance, renewals, transfers, expiry dashboard; plus the fingerprint utility. **Deployed and exercised end to end.** | The whole vendor-side pipeline works before any product depends on it |
| **3** | Wire into WitCloud server: startup, admin UI panel, `/health`, `maxNodes` at registration, expiry → refuse new jobs. Wire into WitSweep: Settings panel, demo, paste field, gate on Run | **Wave 2 can be sold** |
| **4** | `/checkin` endpoint + client-side periodic confirmation (§8.6c) | Revocation for connected customers |
| **5** | *Optional purchase form:* floating licences — instance check-in against a reachable WitCloud, counted per running instance, never per user (§5.3) | Concurrency-based pricing alongside node-locked |
| **6** | Portal self-service: request, download, renew (§8.5) | Scale beyond hand-held sales |

**Product integration is deliberately last** among the load-bearing stages. The
vendor side is where the irreversible decisions live — the key vault, the
issuance flow, the books — and a mistake there is expensive to unwind once
customer installations depend on it. Proving that pipeline against the harness
first means WitCloud and WitSweep are wired against something already known to
work, rather than the two being debugged against each other.

Stages 4–6 follow demand.

### 14.1 Projects

| Repo | Project | Kind | Stage | Published |
|---|---|---|---|---|
| Common *(public)* | `Platform/OutWit.Common.Platform` | **edit** — `IMachineFactorsProvider`, `MachineFactor`, per-probe factor reads; version → 1.1.0 | 0 | nuget.org (existing) |
| Common | `Platform/OutWit.Common.Platform.Tests` | **edit** — factor tests, incl. the multi-OS CI job | 0 | — |
| Common | `Licensing/OutWit.Common.Licensing` | **new** — the core library: format, key ring, verify, binding, store, demo, DI (§11) | 1 | nuget.org, Apache-2.0 |
| Common | `Licensing/OutWit.Common.Licensing.Tests` | **new** — NUnit 4 | 1 | — |
| Common | `Licensing/Samples/OutWit.Common.Licensing.Samples.Avalonia` | **new** — round-trip harness on throwaway keys (§14.3) | 1.5 | — |
| **WitLicense** *(new, private)* | `OutWit.License` | **new** — ASP.NET Core host: WitRPC channels, issuance + signing, key vault, expiry watch | 2 | — |
| WitLicense | `OutWit.License.UI` | **new** — Blazor WASM admin (§12.4) | 2 | — |
| WitLicense | `OutWit.License.Contracts` | **new** — MemoryPack DTOs | 2 | Private feed |
| WitLicense | `OutWit.License.Interfaces` | **new** — WitRPC channel interfaces | 2 | Private feed |
| WitLicense | `OutWit.License.Data` | **new** — EF entity POCOs | 2 | — |
| WitLicense | `OutWit.License.Database` + `.Abstractions` + `.WitDatabase` + `.PostgreSql` | **new** — the template's provider quartet | 2 | — |
| WitLicense | `OutWit.License.Tests` | **new** | 2 | — |
| WitLicense | `OutWit.License.Fingerprint` | **new** — customer-facing desktop utility (§12.5) | 2 | Free download |
| WitCloud | `Cloud/OutWit.Cloud` | **edit** — `AddLicensing`, startup, `/health`, enforcement in `WitNodesManager` + `ProcessingSchedulerService` | 3 | — |
| WitCloud | `Cloud/OutWit.Cloud.UI` | **edit** — licence panel in the admin UI | 3 | — |
| WitSweep | `App/OutWit.Sweep` | **edit** — `AddLicensing`, Settings → Licence, gate on Run | 3 | — |

**WitIdentity is not modified at all** — it only gains an OIDC client
registration for the new admin UI, which is configuration, not code.

Two repos, one boundary: **`Common` holds the format and the verifier and stays
public; `WitLicense` holds the factory and the books and stays private.** The
WitLicense project count looks large only because it is a template clone — nine
of its projects are the standard service skeleton (§12.2).

Plumbing alongside stage 1: `OutWit.slnx` gains a `/Licensing/` folder;
`.github/workflows/ci.yml` gains a paths-filter entry for
`OutWit.Common.Licensing`; `.github/workflows/publish.yml` gains it to the
project choice list; the root `README.md` gains the package.

### 14.2 No CLI

Dropped. The WitLicense admin UI (§12.4) is the tool, and it is a web GUI —
matching how the legacy pair (`Norav.Tools.LicenseGenerator`,
`Norav.Tools.Fingerprint`) was built, how the operator actually works, and how
every other OutWit service is administered.

The one genuinely useful CLI capability — *decode and explain this token* — moves
into the admin UI and the fingerprint utility as a paste box, where it is more
useful anyway because it can render the verdict instead of an exit code.

If batch issuance ever justifies a headless mode, the house pattern is already
established by the Installer families (`…Installer` / `.Core` / `.Cli`): add a
`.Cli` over shared logic, do not fork it.

### 14.3 The sample app — a harness, not a demo

Building the whole loop into a throwaway Avalonia app *before* touching WitSweep
is the right sequencing, for three reasons that a unit-test suite cannot cover:

1. **Cross-platform reality.** Unit tests exercise fakes. Whether `machineId`,
   `osInstall` and `primaryMac` are actually stable across a reboot, a docker
   restart, a VM snapshot, or a macOS update — and whether the store lands in the
   right per-OS directory — is only answerable by running the thing on Windows,
   Linux and macOS. The app is the instrument for that, and stays as the
   permanent manual-test artifact.
2. **The loop has many joints.** keygen → fingerprint → request → issue → deliver
   → install → validate → expire → renew → rotate key. Each joint is where a
   format or path assumption breaks. Better found here than in a shipping
   installer.
3. **It is the conversation piece.** One window that demonstrates the whole model
   to a colleague, or to a customer's IT department asking "what exactly does
   your licensing do to my machine".

Shape: **one window, two panes**, dev-only, never shipped, never signed. It is
**not** the admin UI — it operates entirely on throwaway keys it generates
itself, and knows nothing about the registry. The real tooling is §12.

| Pane | Contents |
|---|---|
| **Product** | Fingerprint + display code; current `LicenseStatus` with the full human description; limits and features as read through `ILicenseService`; a paste field and a file-drop target; a fake **Run** button gated exactly as WitSweep's will be; a "what would happen at *T*" clock-travel control for exercising expiry and `ClockTampered` without waiting a year |
| **Issuer** | Generate throwaway key pairs — several, with different algorithms and product scopes, to exercise §6.3.2; paste a request blob; pick edition / limits / features / term (incl. Unlimited); issue → token; and a deliberately *wrong* issue mode (unknown `kid`, out-of-scope product, mismatched `alg`, tampered payload) to prove every `LicenseStatus` arm is reachable |

Built with `OutWit.Common.MVVM.Avalonia`, house MVVM rules apply — no
code-behind, `ViewModelBase<ApplicationViewModel>`, `UpdateStatus()` gating.
It doubles as the reference integration for the package README, and it exists
**before** `WitLicense` does — so stage 1 can be proven without waiting on the
private repo.

### 14.4 Building WitLicense — clone first, then deviate deliberately

The service is a template clone, so the work is mostly *not* invention. The order
that keeps it that way:

1. **Clone the skeleton from WitAnalytics**, which is the leaner of the two
   blueprints (no Installer family). Host, Contracts, Interfaces, Data, the
   Database quartet, UI, Tests, Dockerfile, compose, Caddy fragment. At the end of
   this step the service builds, starts, signs an operator in over OIDC, and
   serves an empty admin page on 7700/7701.
2. **Then, and only then, the licensing-specific parts**: key vault → issuance →
   registry → dashboard → expiry watch → `/checkin`.

Three places where deviating from the blueprint is correct, and each should be
noted in the repo's own `DESIGN.md` so it reads as a decision rather than a slip:

| Deviation | Why |
|---|---|
| **WitDatabase is the production provider**, not PostgreSql | A few hundred rows a year. The Postgres provider still exists — the template's migration discipline wants two, and parity keeps the provider model honest |
| **No Installer family** | Same reasoning WitAnalytics records: one instance, deployed by us. Revisit only if this ever ships to someone else |
| **A key vault exists at all** | No other service in the family holds signing keys. It brings requirements none of the blueprints have: envelope encryption with the KEK outside the database, an audit row per signature, and backup/restore that is tested rather than assumed |

The last row is the one to be careful about: it is the only genuinely
security-critical component in the service, and it is the one the template offers
no guidance for. Everything else is a clone.

---

## 15. Open questions

**Resolved and no longer open:** edition lists (§6.2.2 — deliberately undecided,
and they decide nothing), transfer policy (§8.6 — soft cap of 3 per year per
licence plus a recorded reason; exceeding it is an explicit operator action that
lands in the audit log), and `checkIn` defaults (§8.6c — per product: on for
desktop clients, off for the on-prem server, 30-day grace, always visible in the
licence panel).

Still open:

1. **Version policy on major upgrades** — does a 1.x licence cover 2.0? `appVer`
   supports either answer, and it is the recommended shape for *Unlimited*
   licences (§6.2.1), so the commercial choice should be made before stage 3
   builds the issue form.
2. **Trial self-service** — can a prospect download WitSweep and get a 30-day
   demo with no contact at all (current design), or must a trial be requested?
3. **Multi-seat purchase ergonomics.** A customer buying 12 workstations sends 12
   fingerprints and receives 12 documents. Acceptable at small scale; if it is
   the common case, the admin UI needs a batch flow (import many blobs → issue a
   set → deliver one archive).
4. **Which product lines get which key custody** (§12.2). Only server custody is
   implemented; the question is whether any line ever justifies more.

None of these block stages 0–2.
