# OutWit Licensing — the verifying side

> **Status: draft for review (2026-08-08).** The counterpart to
> [`DESIGN.md`](DESIGN.md) (format, crypto, flows) and
> [`WitLicense/DESIGN.md`](../../WitLicense/DESIGN.md) (the factory and the
> books). Those two describe how a licence is *made*; this one describes how a
> product *behaves* when it reads one — the enforcement model, the mock apps
> that prove it, and the integration plans for WitCloud and WitSweep.
>
> The issuing half is built and deployed (`license.omnibuscloud.com`, v1.0.5,
> `OutWit.Common.Licensing` and `OutWit.Common.Platform` on nuget.org). Nothing
> in this document is code yet.

---

## 1. What this covers

Stage 3 of [`DESIGN.md`](DESIGN.md) §14 — "wire into WitCloud and WitSweep" —
is one line in the staged plan and roughly two and a half months of work
(§12.2). It is one line because the *mechanism* was fully specified; what was
never specified is the **behaviour**: what a product does at the moment the
answer is "no".

This document defines:

- the border between *"this program may run here"* and *"this person is signed
  in"*, and the third thing that is neither (§2);
- the operating modes a product moves through, and what degradation actually
  means for a server and for a desktop app (§3);
- how grace, the clock guard and n-of-m binding behave in practice, including
  the cases where they will misfire (§4);
- what the library is missing to express any of this (§5);
- the mock applications that prove it before a shipping product is touched (§6);
- **the integration kit** — two delivery components over one core, so that
  licensing a further *service* costs five lines and licensing a further *client*
  costs a view and an options block (§7);
- integration plans, separately, for the client family (§8 — WitSweep) and the
  service family (§9 — WitCloud and WitIdentity);
- the work this lands back on the issuing side (§10);
- what aspects, obfuscation and source generation are each worth here — three
  tools with three different answers (§11);
- the staged plan, an audit of it, and what to cut under pressure (§12);
- what is still open (§13).

Out of scope: `checkIn` (stage 4), floating licences (stage 5), the portal
(stage 6). All three are reserved in the format and none is needed to sell.

---

## 2. The border — three levels, not two

[`DESIGN.md`](DESIGN.md) §5 establishes two orthogonal axes: **identity**
(WitIdentity — who you are, which fleet) and **entitlement** (the licence — what
may run on this machine). That is correct and it is the load-bearing rule, but
it is not a complete answer to "where does the check go", because it says
nothing about *which verbs* the entitlement axis governs.

Enforcement has three levels, and every enforcement point in either product
belongs to exactly one of them:

| Level | Governed by | Verbs |
|---|---|---|
| **0 — Presence** | Nothing. Never gated, ever | Launch, open, read, inspect, export, request a licence, install a licence, sign in, sign out, admin UI, `/health` |
| **1 — Entitlement** | `ILicenseService.State.CanRun` — machine/deployment, offline, no principal | The productive verb: *submit a sweep*, *accept a new job*, *register a new node* |
| **2 — Attributes** | `HasFeature(key)` / `Limit(key)` within a valid licence | Feature flags and caps: `maxNodes`, `maxVariants`, `sso`, `format.nas` |

Identity sits **beside** level 1, never above or below it. For WitSweep both
must pass to submit a sweep, and they answer different questions with different
fixes. For WitCloud they do not even meet: the licence is a property of the
deployment, the caller's token is a property of the caller, and no code path
consults both to answer one question.

### 2.1 The rule that keeps them apart

> **A refusal must name its own axis.** *"No licence for this workstation"* and
> *"you are not signed in"* are different sentences, shown in different places,
> fixed by different people. A message that blurs them costs a support cycle
> every time.

This is why `ILicenseService` takes no principal (`ILicenseService.cs`), why
`LicenseState` carries no user, and why the refusal path returns
`LicenseValidationResult` rather than a `bool`. It is also the first thing to
check in review of any integration commit.

### 2.2 What level 0 buys, and why it is not generosity

Level 0 is not a courtesy to lapsed customers. It is what makes the rest
enforceable:

- A customer whose licence lapsed still owns their data ([`DESIGN.md`](DESIGN.md)
  §5.4) and must be able to export it. Holding data hostage converts a renewal
  conversation into a legal one.
- **The fix for a licensing failure is reachable only through level 0.** A
  server that refuses to start cannot show the panel where the operator would
  paste the licence. A desktop app that refuses to open cannot show its own
  fingerprint. Gating level 0 makes the system unrecoverable by exactly the
  person who is trying to pay.

---

## 3. Operating modes — what "degrade" actually means

`LicenseState.CanRun` is currently `Status == Valid` — a boolean. That is
enough to gate a verb and not enough to express any of the behaviour
[`DESIGN.md`](DESIGN.md) §2 committed to ("degrade — refuse new work, finish
current"). The products need a named mode, derived once and read everywhere.

```csharp
public enum LicenseMode
{
    Licensed,      // valid licence in force
    Demo,          // self-issued, within its term
    Grace,         // term ended, inside the product's renewal grace
    Restricted,    // no entitlement — level 0 only
}
```

| Mode | Entered when | Level 1 verbs | What the user sees |
|---|---|---|---|
| `Licensed` | `Valid` | Allowed | Nothing, until 30 days from expiry — then a banner that escalates |
| `Demo` | No licence ever installed, demo term running | Allowed, under demo caps | Persistent, non-modal "Demo — N days remaining" |
| `Grace` | `Expired` and `now < exp + grace` | **Allowed** | Prominent, escalating banner naming the exact date and the `jti` |
| `Restricted` | Everything else: grace exhausted, `BindingMismatch`, `ClockTampered`, `WrongVersion`, demo over, `Missing` after a licence once existed | **Refused, with the specific reason** | The reason, the fingerprint, and the way to fix it |

Three properties of this table are deliberate:

1. **There is no `Blocked` mode.** Nothing in either product ever refuses to
   start, and nothing terminates work already running. `Restricted` refuses the
   *next* productive verb and nothing else (§2.2).
2. **`Grace` still allows work.** A grace window that refuses work is just a
   later expiry with a worse message. Its whole purpose is that a lapse
   discovered at 3am on a Sunday does not stop a production cluster; it makes
   noise instead.
3. **`Restricted` is not one message.** It carries the underlying
   `LicenseStatus`, so "your licence expired on 2027-08-05" and "this licence
   was issued for a different machine" stay distinct.

### 3.1 Renewal grace is a product-side policy, not a payload field

There are two different graces in this system and conflating them would be a
mistake:

| | Renewal grace | Check-in grace |
|---|---|---|
| Answers | "`exp` passed and no new document arrived yet" | "the registry is unreachable" |
| Source | **The product build** — `AddLicensing(o => o.WithGrace(...))` | **The payload** — `checkIn.graceDays`, chosen per licence by the admin |
| Stage | Now | 4 |
| Default | `TimeSpan.Zero` | 30 days |

Renewal grace belongs to the build because it is a *product promise*, uniform
across customers, and because putting it in the payload would mean every issued
licence silently carries a term longer than the one on the invoice. Proposed
values:

- **WitCloud server: 14 days.** The 3am argument. An on-prem cluster mid-run
  when a PO is late must not stop.
- **WitSweep: 0 days.** A person is sitting in front of it. Level 0 already
  keeps their data reachable, the banner has been escalating for 30 days, and
  the fix is one paste. A desktop grace mostly extends every licence for free.

Both are one constructor argument, so either can be revisited without a format
change. Both must be **disclosed in the licence panel** — a grace nobody knows
about produces the same 3am support call it was meant to prevent, one fortnight
later.

### 3.2 The clock guard needs to stop accusing people

`ClockGuard` (`Storage/ClockGuard.cs`) keeps a high-water mark and reports
`ClockTampered` when the clock reads more than 24h behind it. The mechanism is
right. Two things about its current handling are not:

- **It discards the licence.** `LicenseService.ReloadAsync` returns a
  `ClockTampered` state with **no payload**, so the panel cannot say *"your
  Enterprise licence for ACME GmbH is fine — this machine's clock reads
  2019-03-11"*. It can only say "clock tampered", to a customer whose CMOS
  battery died. The licence should still be evaluated and the clock reported as
  a **modifier** on the state, not as a replacement for it.
- **It reads as an accusation.** The legitimate causes — a VM restored from a
  snapshot, a dead RTC, a laptop returning from a badly-configured timezone, a
  fresh container with no NTP yet — are all more common than the illegitimate
  one. The message must describe the observation, not the motive: *"the system
  clock is N days behind the last time this product ran; licences cannot be
  checked until it is corrected."*

It stays a `Restricted` trigger — it is the free bypass and must gate level 1 —
and it is **self-healing**: the next evaluation after the clock is corrected
returns to `Licensed` with no support call and no reissue.

### 3.3 What each product refuses, concretely

| | WitSweep | WitCloud (on-prem) | Inventor add-in |
|---|---|---|---|
| What starts | The application | The service | **Autodesk Inventor. Not us** |
| Never affected | Launch, open a deck, edit parameters, view and export results, sign in, licence panel | Start, `/health`, admin UI, sign-in, dashboards, accounting export, licence panel, **running jobs finish** | Inventor itself, the add-in loading, the ribbon appearing, the settings dialog — fingerprint, request export, licence paste |
| `Restricted` refuses | Submit a sweep | Accept a **new** job; register a **new** node | Invoke the add-in's functions |
| Failure surface | A dialog on the attempted action **plus** a persistent banner | A banner in the admin UI, a field on `/health`, a log line at `Warning`, and a licensing-specific `Result.Rejected` reason on the refused call | The ribbon command is **visibly disabled and says why** — never a silent no-op |
| Who fixes it | The engineer at the keyboard | An operator who may be asleep — hence the grace and the 30-day warning | The engineer at the keyboard |

The asymmetry the user asked about resolves as: **no product refuses to start.**
For WitCloud the answer is not "refuse start *or* degrade to read-only", it is
degrade — refusing to start is forbidden by §2.2, because the admin UI is where
the licence gets pasted. WitSweep is the same rule from the other end: it must
launch, and what it withholds is the productive verb, never the customer's
access to work they already paid for.

**The add-in makes that rule physical rather than chosen.** There is no start
moment to refuse: Inventor loads the add-in, and by then the host is already
running. Everything the design says about level 0 therefore applies to it
without an argument being needed — the settings dialog is reachable because
nothing was ever in a position to prevent it.

That third column carries one hazard the other two do not. "The functions are
simply not called" is the natural way to implement a disabled add-in and the
worst possible failure surface: the user clicks, nothing happens, and they
report a broken plugin. A refusal must name its own axis (§2.1), so a
`Restricted` add-in disables its commands **visibly**, with the reason on the
control, and offers the settings dialog as the way out.

---

### 3.4 `WrongVersion` is a scope mismatch, not a lapse — and it needs a guard

`appVer` is the safety valve that makes an unlimited term reasonable
([`DESIGN.md`](DESIGN.md) §6.2.1): perpetual for the version that was bought, a
new document for a major upgrade. The mechanism exists and works
(`LicenseVersionRange` — space- or comma-separated clauses over `>= <= != > < =`,
all of which must hold, empty range matches everything). What was never decided
is how the *product* behaves when a customer crosses the ceiling, and the naive
answer is bad enough to be worth naming.

**The failure it must not have:** a customer with a valid, unexpired, possibly
*unlimited* licence runs a routine upgrade and discovers a `Restricted`
production server. They did nothing wrong, nothing expired, and the system they
were entitled to run is gone until someone answers an email.

Four rules follow:

1. **Never treated as expiry.** No grace — grace is a time concept and no amount
   of waiting fixes a version mismatch. `WrongVersion` goes straight to
   `Restricted` with its own sentence: *"this licence covers 1.x; version 2.0 is
   installed — reinstall 1.x, or renew to cover 2.x."* Two actions, both real.
2. **The upgrade is guarded before it happens.** This is the part that turns the
   worst licensing experience into a non-event: the installer checks the
   installed licences against the version it is about to lay down and refuses,
   naming the licence. WitCloud has an Installer family; WitSweep ships an
   installer. For the Docker path there is no installer, so the container **logs
   loudly at startup and runs `Restricted`** — it must not crash, or §2.2 is
   violated at the worst possible moment.
3. **A bounded range makes `ProductVersion` mandatory.** `Matches(null)` returns
   `false` for any bounded range, deliberately — a product that cannot state its
   own version cannot claim to be inside a bounded one. Since §10.4 makes bounded
   the *default*, every product must pass a real version to `ForProduct(...)`.
   WitSweep has `SweepVersionInfo`; WitCloud and WitIdentity must pass their
   assembly version. Forgetting it is a first-deployment `WrongVersion` on a
   perfectly good licence.
4. **Pre-release suffixes do not exist here.** `System.Version` has no
   pre-release component, so `1.5.0-beta` compares exactly as `1.5.0` — and
   WitCloud ships `v1.5.x-beta` today. A range cannot exclude a beta, and nobody
   should design as though it could.

One property of the parser deserves to be stated rather than discovered: **a
malformed clause is dropped, and a wholly malformed range matches everything.**
That is deliberate and right — a customer must never be dead because of a typo
they cannot see or fix — but it means the range **fails open**, so the *issuing
form* is the only place that can catch it (§10.5).

---

## 4. Binding in practice — where n-of-m will actually misfire

### 4.1 The two shapes as built

| | Clients (WitSweep) | Services (WitCloud, WitIdentity) |
|---|---|---|
| Provider | `LicenseBindingProviderMachine` | `LicenseBindingProviderTenant` |
| Factors | `machine-id`, `primary-mac`, `machine-name` | `installId`, `publicBaseUrl`, `issuer` — **proposed change, §7.8.1–§7.8.3** |
| Threshold | **2 of 3** — tolerant, hardware drifts | **3 of 3** — strict, a deployment does not drift |
| Unit sold | A workstation | **An instance** |

The thresholds run in opposite directions on purpose. A workstation's factors
degrade one component at a time through legitimate maintenance, so the binding
must absorb drift. A deployment's do not: a container that is recreated,
upgraded or moved to new hardware keeps every factor exactly, so anything that
*does* change is a different deployment. §7.8.3 has the selection rule and the
full candidate list.

Two corrections to [`DESIGN.md`](DESIGN.md) §7.1, both found by reading the code
rather than the document:

- It names the desktop factors `machineId / osInstall / primaryMac`. What
  `OutWit.Common.Platform` actually provides is
  `machine-id` / `primary-mac` / `machine-name` (`MachineFactorKeys.cs`) — there
  is no OS-install factor. That matters for §4.2.
- It specifies the server binding as `tenant` + `installId` at 1-of-2. The
  `tenant` factor has no source to read from, and at threshold 1 it makes a
  licence portable to any deployment sharing the slug — which is the threat the
  binding exists to catch. §7.8.1 has the full argument and the recommendation:
  bind on `installId` alone, keep `tenant` as a record rather than a factor.

Both rows change what a licensed unit *is* for services: not a deployment, an
**instance**. A stack of two services is two licences, and a second WitCloud is a
third.

### 4.2 Where 2-of-3 is weaker than it looks

Honest accounting, because the alternative is discovering it from a customer:

| Event | Factors still matching | Verdict |
|---|---|---|
| RAM/GPU/disk upgrade | 3 | Works — this is the case n-of-m exists for |
| NIC replaced, or docking station swapped | 2 (`machine-id`, `machine-name`) | Works |
| Host renamed by IT | 2 (`machine-id`, `primary-mac`) | Works |
| OS reinstalled in place | 2 (`primary-mac`, `machine-name`) | Works — arguably too generous, but the alternative punishes a legitimate rebuild |
| Different machine, same corporate naming scheme (`WKS-0042`) | 1 | Refused. Correct |
| **VM cloned wholesale** (snapshot restored elsewhere) | **3** | **Works on both.** The licence is duplicated |
| Wi-Fi-only laptop with per-network MAC randomisation | 2 on a good day, 1 on a bad one | **Intermittent refusals** |
| VDI / pooled desktop | 0–1 | Refused, always |

Two of these need a decision now rather than later:

- **Wi-Fi-only hosts.** `primary-mac` prefers wired adapters, but a modern
  laptop may have none. If the MAC is randomised per network, the licence
  oscillates between 2 and 1 matches — which is the worst possible failure,
  because it is intermittent. *Proposed mitigation:* when
  `MachineFactorsProvider` yields fewer than three usable factors, the request
  blob says so, and the admin issues at threshold 1 with `machine-id` as the
  sole factor. This needs a **threshold field on the issue form** (it exists in
  the payload; §10 lists the UI work).
- **VDI and pooled desktops.** 2-of-3 cannot work there and no tuning fixes it.
  *Proposed answer:* WitSweep is not sold for VDI. If a customer runs it there
  anyway, they get `kind: none` with a short term and a renewal cadence — the
  contract does the work the binding cannot.

  **This answer does not transfer to the Inventor add-in, and the difference is
  the audience, not the technology.** "We do not sell for VDI" is a position one
  can hold about a tool a customer chooses to install; it is much harder to hold
  about an add-in for a CAD package whose seats their IT department has already
  standardised on Citrix or a pooled workstation image. The add-in also faces
  routine re-imaging from a corporate template, which is a rebuild the machine
  factors read as a different machine. So **the add-in's factor set is chosen on
  its own evidence, not copied from WitSweep** — and the rule in §7.8.3 still
  decides it: a factor earns its place only if lying about it breaks the
  deployment. Where that leaves fewer than three usable factors, the threshold
  field from the bullet above is the mechanism, not an exception to it.

The wholesale-VM-clone case is unfixable offline and is already covered by
[`DESIGN.md`](DESIGN.md) §3.2. It stays in the docs, not in the code.

### 4.3 What `installId` actually is — and how it is obtained

**It does not exist yet.** `LicenseBindingProviderTenant` takes it as a
constructor argument and `FACTOR_INSTALL_ID` names it, but nothing in the
codebase generates, persists or reads one. It is a design concept from
[`DESIGN.md`](DESIGN.md) §7.2 whose implementation is unwritten work.

**What it is:** 128 random bits, generated once when a deployment is created and
never changed for the life of that installation. It is the container-safe
substitute for a hardware fingerprint, and it exists for one reason: inside a
container `/etc/machine-id` is not stable across recreation, so
`MachineIdentityProvider` would hand out a different identity after every
`docker compose up --force-recreate`. A random value the *installation* owns has
none of that problem.

It identifies **an installation**. Not a machine — the container can move hosts.
Not a customer — that is the `customer` block. Not a tenant — that is a name.

**Where it comes from: `.env`, generated at deploy time** (decided — §7.8.3),
as `Licensing__InstallId`, beside the other generated values the installer
writes. It is configuration, not state.

```
# .env, written once by the installer alongside the database password
Licensing__InstallId=b3f1c7a94e0d…
```

**Fallback when it is unset** — a hand-rolled `docker compose up`, a developer
machine, the container mock: generate 16 bytes and persist them to
`<licenseDir>/install-id`, atomically, read-if-present thereafter. Both forms
ship; the installer always sets the env var, so the file form is what
zero-configuration environments fall back to.

**Four properties, each of which is a failure if violated:**

| Rule | What happens if broken |
|---|---|
| **Never baked into the image** | Every deployment from that image shares one identity — one licence silently covers every customer. Catastrophic and invisible |
| **Generated once per deployment, never on start** | Regenerating makes every restart a new installation |
| **Preserved across upgrades** | An upgrade that rewrites `.env` from the template destroys the identity. The same is already true of the database password, so the discipline exists — it just has to cover one more key |
| **Not treated as a secret** | It is an identifier, not a credential. Nothing breaks if an operator reads it; the licence records only its hash |

Three properties come free from configuration that neither a volume nor a
database would have given: it is **available before the host starts**, so
licensing evaluates at composition time like everything else; **replicas of one
deployment share it automatically** with no race to resolve; and it is
**knowable before first start**, so a customer can request a licence while
installing rather than after.

Two things follow for the deployment layer, and both are documentation rather
than code:

- **The installer declares the volume, not just the image.** A stack that comes
  from `OutWit.Cloud.Installer` should carry the licence volume in its compose
  file and in its backup story, alongside the database.
- **`INSTALL.md` must say so.** "Back up `/app/license/`" belongs beside the
  database backup instructions, or the first disaster-recovery drill produces a
  support ticket instead of a running cluster.

---

## 5. What the library is missing

The verifying half is written and tested, but it was written against unit tests
and a harness, not against a long-running host or a bindable UI. Working
through §3 and §8–§9 turned up eight gaps. All are small; several are blocking.

| # | Gap | Why it blocks | Size |
|---|---|---|---|
| 1 | **No `StateChanged` event on `ILicenseService`** | The house `UpdateStatus()` pattern has nothing to subscribe to; a banner cannot refresh; `CanRun` cannot re-gate a command | Small |
| 2 | **No periodic re-evaluation** | `ReloadAsync` is only ever called by hand. A server up for four months crosses `exp` and never notices until it restarts — and this is **not only a server concern**: an Inventor session runs for days and a draughtsman does not restart their CAD, so a client add-in crosses `exp` mid-session exactly the same way | Small — a timer, with an `IHostedService` arm for services and a plain timer for clients |
| 3 | **No `LicenseMode` / grace** (§3) | `CanRun` cannot express "expired but inside grace"; the products would each invent their own | Medium |
| 4 | **No env-var or composite store** | [`DESIGN.md`](DESIGN.md) §13 promises `Licensing__License` for Docker. Only `LicenseStoreFile` and `LicenseStoreMemory` exist | Small — `LicenseStoreComposite` + `LicenseStoreEnvironment` |
| 5 | **No uninstall on `ILicenseService`** | `ILicenseStore.Remove` exists; the service does not expose it, so the panel cannot remove a superseded document | Trivial |
| 6 | **No convenience surface for the panel** | Every UI must reach into `State.Payload` for expiry, days remaining, customer, edition label. Four properties on `LicenseState` remove that from three codebases | Trivial |
| 7 | **`LicenseState.Describe()` calls `DateTime.UtcNow` directly** (`LicenseState.cs:45`) | Ignores the injected clock, so the harness's clock travel reports demo days that disagree with the state it is describing | Trivial — bug |
| 8 | **No non-DI factory** | WitSweep composes by hand in `App.axaml.cs`, with no `IServiceCollection`. `AddLicensing` is the only path that performs the initial `ReloadAsync` | Trivial — `Licensing.Create(o => …)`, with `AddLicensing` calling it |

Gaps 1–4 are the substance of the library work in this phase; 5–8 are an
afternoon. All are additive — no format change, no reissue, no breaking change
to the published package. Target: `OutWit.Common.Licensing` 1.1.0.

**Two further gaps were raised and then withdrawn**, and the round trip is worth
recording rather than erasing. Holding identity in the database would have
required an async `ILicenseStore` and a *not-evaluated-yet* licence state, since
a service cannot read its database at `ConfigureServices` time (§9.8). Choosing
configuration instead (§7.8.3) removed both. The lesson is not that the analysis
was wasted — it is that **a storage decision taken three exchanges after the
library was declared finished added two items to it, and reversing the decision
removed them.** Where two options are otherwise close, prefer the one whose
consequences stay outside the shared library.

---

## 6. Proving it on mocks first

The issuing side was built against a harness before it was built against a
product, and that ordering paid for itself three times over
(`Samples/…/README.md` lists the defects it caught on its first run). Same
discipline here, with one addition.

### 6.1 The Avalonia harness — extend, do not replace

**Verdict: it is the right instrument and it stays.** It already closes
keygen → fingerprint → request → issue → install → validate → expire → refuse,
with clock travel and nine deliberate defect modes covering every
`LicenseStatus` arm. Nothing about the verifying phase justifies starting over.

What it does not yet do, and needs to:

| Addition | Why |
|---|---|
| **Mode display** — `Licensed / Demo / Grace / Restricted`, not just a status string | §3 is the new thing being designed; it has to be visible or it is not being tested |
| **Real key ring** — load `witsweep.keyring.json` exported from WitLicense, alongside the throwaway keys | The export format (`WitLicense/DESIGN.md` §7.2) has never been consumed by anything. Until a verifier reads one, it is unproven |
| **Real token round trip** — paste a licence issued by `license.omnibuscloud.com`, delivered by email | Closes the first full production cycle, which has never been run end to end. Also the first real Resend send |
| **Renewal overlap** — install a staged `NotYetValid` renewal beside a live licence and watch the switch at `exp` | `LicenseService.Evaluate` implements best-valid selection and supersession; nothing has ever watched it happen |
| **Uninstall** | Gap 5, and the only way to test a document being removed rather than superseded |

Note what this buys beyond testing the library: **it discharges four items from
the operational tail** — the never-run production cycle, the unverified Resend
transport, the unconsumed key-ring export, and the untested `.owlreq` import.

### 6.2 A second mock, containerised — the one the Avalonia app cannot be

The Avalonia harness cannot answer the server-shaped questions, and those are
the ones with the expensive failure mode:

- Does `installId` survive `docker compose up --force-recreate`? (§4.3)
- Does `Licensing__License` as an env var work alongside a `.lic` file drop, and
  which wins?
- Does the store land somewhere writable in a container running as non-root?
- Does the licence apply **without a restart**, as [`DESIGN.md`](DESIGN.md) §8.1
  requires?
- Does the tenant slug read the same way it does in WitCloud, from `tenant.json`
  mounted at runtime?

**`OutWit.Common.Licensing.Samples.Server`** — a minimal-API host, ~150 lines
plus a compose file, exposing `/health` (with the licence field), `/license`
(state, fingerprint, request blob) and `POST /license` (install). No database,
no identity, no WitRPC. It is the WitCloud *shape* with none of the WitCloud
*substance*, and it can be destroyed and recreated in a loop.

This is a small, high-leverage artifact: it de-risks the single decision in the
design most likely to produce a silent, delayed, customer-visible failure.

### 6.3 What is *not* worth mocking

No mock for WitIdentity interaction — there is none by design (§2). No mock for
the admin UI — WitLicense is deployed and is the real thing. No mock for the
worker client or the addons — they carry no licensing code and never will
([`DESIGN.md`](DESIGN.md) §10.4).

---

## 7. The integration kit — two families, one core

### 7.1 The observation that decides the whole shape

Counting the work honestly: the **gate is two to six lines per product**
(§11.3). The **panel is a screen**. Everything expensive about licensing a
product is the panel, so "simple and uniform" means exactly one thing:

> **Licensing product N+1 must cost one options block, one thin view, and the
> enforcement points that are genuinely its own business logic. Nothing else.**

That is the test `WitLicense/DESIGN.md` §2 sets for the issuing side ("adding a
third product must not require a code change"), applied to the verifying side.
It is worth stating because the naive path — every product writes its own panel
against `ILicenseService` — produces N divergent renderings of the same
eleven-arm status vocabulary, and the third one is where the arms start going
missing.

### 7.2 Two consumer families, and why they cannot share one component

There are two shapes of consumer, and they differ in the one dimension that
matters:

| | **Services** — WitCloud, WitIdentity, WitForms, WitAnalytics, WitLicense | **Clients** — WitSweep, the Inventor add-in, future desktop apps |
|---|---|---|
| Built from | The OutWit product template: Kestrel + WitRPC + Blazor WASM admin + OIDC | Nothing shared — **Avalonia in WitSweep, WPF in the add-in**, whatever next |
| Panel runs in | **The browser** | The app process |
| `ILicenseService` lives in | **The host** | The same process |
| Distance to the licence | A WitRPC round trip | A field |
| UI technology | **MudBlazor, uniformly** | Different in every product |
| Binding | Tenant / deployment | Machine |

The two rows in bold are what force the split. A service's panel **cannot** hold
an `ILicenseService` — the browser has no machine factors, no store and no key
ring, and must not have them. Meanwhile services share a UI technology and
clients do not, so the shareable surface is a **finished, themed component** for
services and **everything except the view** for clients.

So: two delivery components, one shared core underneath. Not two
implementations.

### 7.3 The layering

```
  OutWit.Common.Licensing            verifier · format · binding · store · LicenseSnapshot
            │
  OutWit.Common.Licensing.MVVM       ILicenseGateway · LicensePanelViewModel ·
            │                        LicenseGatewayLocal · clipboard + file seams
            │                        NO UI framework, NO transport
            │
            ├──────────────── CLIENTS ────────────────┐
            │   WitSweep: own Avalonia view           │
            │   + LicenseGatewayLocal                 │
            │                                          
            └──────────────── SERVICES ───────────────┐
                OutWit.Shared.Licensing.Contracts      │  ILicenseChannel + MemoryPack DTOs
                OutWit.Shared.Licensing                │  host: channel base, AddServiceLicensing
                OutWit.Shared.Licensing.Blazor         │  MudBlazor views + LicenseGatewayChannel
                    → WitCloud, WitIdentity, WitForms, WitAnalytics, …
```

**`ILicenseGateway` is the joint that makes one panel ViewModel serve both
families.** Local implementation for clients, channel implementation for
services; the panel never knows which it has. Without that abstraction the two
families would need two panels, and the eleven-arm vocabulary would be written
twice.

Two supporting types carry the weight:

- **`LicenseSnapshot : ModelBase`** — a flat, JSON-serialisable projection of
  `LicenseState`: mode, status, the `Describe()` sentence, expiry, days
  remaining, customer, edition label, `jti`, fingerprint, granted features and
  limits, unrecognised keys, installed documents, grace policy in words. It
  lives in **`OutWit.Common.Licensing`**, not in the MVVM package, because
  `/health` wants it too. `LicenseState` deliberately cannot cross a wire — it
  holds a `LicenseValidationResult` and has an internal constructor — and a
  projection is what a panel binds to anyway. This supersedes gap 6 in §5.
- **`LicensePanelViewModel : ModelBase`** — composition, never inheritance. It
  must not derive from a platform ViewModel base, because service page VMs
  derive from `AutoRefreshViewModel` (MudBlazor) and WitSweep's from
  `ViewModelBase<ApplicationViewModel>`. Each consumer *holds* it and binds to
  it. `ModelBase` already implements `INotifyPropertyChanged`, so this is free,
  and `[Notify]` + `RelayCommand` from `OutWit.Common.MVVM` bind identically in
  Avalonia, WPF and Blazor.

Two platform seams, both optional and defaulted to no-ops, because copy and save
are the only two things a panel needs that are not portable:

```csharp
public interface ILicenseClipboard { Task SetTextAsync(string text); }
public interface ILicenseFileTransfer
{
    Task<string?> OpenTextAsync();                       // pick a .lic
    Task SaveTextAsync(string fileName, string content);  // save a .owlreq
}
```

WitSweep already has `IClipboardService` and `IFilePickerService`; the Blazor
side already has the JS-interop download path. Each adapter is about ten lines.

**Correction from building it: there is a third seam, and unlike these two it is
not optional.** `LicensePanelViewModel` also takes an `IDispatcher` (already in
`OutWit.Common.MVVM`, framework-neutral), and **every consumer with a UI thread
must supply one.**

The reasoning that hid this is worth recording, because it is plausible and
wrong. A panel that *awaits* the gateway does come back to the thread that
asked, so the command path looks safe. But `ILicenseService.StateChanged` is not
awaited by anybody: the runtime raises it from wherever its own re-evaluation
finished, and every await inside `OutWit.Common.Licensing` suppresses the
synchronization context **by design**, so that a desktop host can block on its
first evaluation without deadlocking (§5 gap 8). The two decisions compose into
a snapshot that arrives on a thread-pool thread — and it arrives that way after
an **ordinary install**, not only when the periodic re-evaluation of gap 2 is
switched on.

The symptom, seen the first time the migrated harness was run rather than in any
test: a licence installed correctly and the screen showed half of it. Four
properties updated, the collections faulted, and the notification layer — which
dispatches subscribers through reflection — re-wrapped the whole thing as
*"Exception has been thrown by the target of an invocation"*, a sentence naming
neither threading nor licensing.

Three consequences, all now closed:

- The panel takes the seam, documents it as required, and marshals through it.
- `LicenseGatewayLocal` deliberately does **not** suppress the context, which is
  the one place in this stack where capturing it is right: a gateway is the
  boundary a view model awaits.
- The panel unwraps to the innermost exception before showing one. A licence
  panel is the screen a customer reaches *because* something is already wrong;
  it is the last place that may report a reflection detail instead of a cause.

This is exactly what §7.6 predicted the harness would buy — a defect in the
abstraction, found while the abstraction was still free to change, by a consumer
rather than by a test.

### 7.4 Component 1 — services: `OutWit.Shared.Licensing.*`

This is a **direct clone of the precedent that already works**:
`OutWit.Shared.Logging.Blazor` is a neutral MudBlazor component set over a
neutral contract (`ILogQueryProvider`), consumed by WitCloud through a five-line
page. Licensing gets the same treatment, one layer deeper, because unlike
logging it also needs a host side.

Home: the **`Shared` repo**, under `Licensing/`, beside `Logging/` and `Email/`.
Apache-2.0, like everything there.

| Package | Contents | Blueprint |
|---|---|---|
| `OutWit.Shared.Licensing.Contracts` | `ILicenseChannel` + MemoryPack DTOs (`LicenseInfo`, `LicenseSummary`, `LicenseRequestInfo`, `LicenseInstallResult`) | `OutWit.Cloud.Contracts` |
| `OutWit.Shared.Licensing` | Host side: `LicenseChannelBase`, `AddServiceLicensing(...)`, tenant/installId binding, composite store, health contribution | — |
| `OutWit.Shared.Licensing.Blazor` | RCL: `LicensePanel`, `LicenseBanner`, `LicenseRequestDialog`, `LicensePageViewModel`, `LicenseGatewayChannel` | **`OutWit.Shared.Logging.Blazor`** |

**The contract package is the load-bearing one.** Putting `ILicenseChannel` in
each service's own `Contracts.Internal` — which is what §9 originally proposed —
would mean five near-identical channel interfaces and five near-identical DTO
sets, and the Blazor component could not be shared at all because it would have
no common type to talk to. One contract for the whole family is what makes the
component possible.

The one genuine seam is authorisation: `EnsureAdmin()` is private to each
service and reads that service's own `PrincipalStore` and role constants. So
`LicenseChannelBase` is `[InjectableHost]` and abstract on exactly that:

```csharp
public abstract partial class LicenseChannelBase : ILicenseChannel
{
    protected abstract Result<T>? EnsureOperator<T>();
    // everything else — get, install, request, remove, summary — is shared
}
```

Per service, the whole cost:

```csharp
// Startup.cs — two lines
services.AddServiceLicensing(o => o
    .ForProduct("WitForms", ThisAssembly.Version)
    .WithTenant(ConfigurationService.Tenant.Slug)
    .WithGrace(TimeSpan.FromDays(14)));
srv.AddService<LicenseChannel>();          // a ~10-line subclass supplying EnsureOperator
```

```razor
@* Views/Pages/License.razor — the whole page *@
@page "/license"
@inherits LicensePageViewModel
@attribute [Authorize]
<LicensePanel />
```

Plus a nav entry, plus the enforcement points — which are genuinely per-service
business logic and must stay hand-written (§7.7).

### 7.5 Component 2 — clients: `OutWit.Common.Licensing.MVVM`

Home: the **`Common` repo**, `Licensing/OutWit.Common.Licensing.MVVM`.

Its defining constraint is what the request named: **no UI dependency at all.**
Not Avalonia, not WPF, not Blazor, not MudBlazor. Its only dependencies are
`OutWit.Common.Licensing`, `OutWit.Common.MVVM` (the base package — `ModelBase`,
`RelayCommand`, framework-neutral by construction) and `OutWit.Common.Aspects`.

It gives a client:

- `LicensePanelViewModel` — every property, command and enablement rule the panel
  needs, already computed: the status sentence, days remaining, the severity a
  banner should use, whether *Install* is enabled, what a failed paste said.
- `LicenseGatewayLocal` — wraps the in-process `ILicenseService`.
- `Licensing.Create(o => …)` — the non-DI factory (gap 8), because desktop apps
  compose by hand.

It does **not** give a view, and that is deliberate (§7.7). A client's job is to
bind a view of its own design to a ViewModel that already knows everything.

### 7.6 The harness is the first consumer, not the last

The harness's Product pane (`Samples/…/ViewModels/ProductViewModel.cs`, 289
lines) is already a working prototype of exactly this panel. **It migrates to
`LicensePanelViewModel` before any shipping product adopts it**, and its
behaviour must be unchanged afterwards.

This is not tidiness. It exercises the abstraction under a real consumer while
it is still free to change, and it keeps the harness proving the panel and the
library together instead of drifting into a private re-implementation of the
same eleven statuses.

### 7.7 What is deliberately kept out of both components

**The gate.** Six sites, six failure shapes, six product-specific sentences
(§11.3). Abstracting it would produce exactly the generic refusal §2.1 forbids
and would hide the one thing a reviewer must be able to read at a glance.

**Client views.** After this phase there are three: the harness, WitSweep, and
whatever comes next. If they turn out near-identical, `…Licensing.Avalonia`
becomes a mechanical extraction later; packaging one now would be guessing the
shape from a single example. Note this does **not** apply to the service family —
there the view *is* shared, because every service on the template renders
MudBlazor and the panel is the same screen in all of them.

**Banner placement.** The panel VM supplies text and severity; where the strip
hangs is a layout decision. `LicenseBanner` exists in the Blazor package because
services share a layout; clients place their own.

### 7.8 One deployment, several licensed services

**Decided:** an on-prem WitCloud installation ships with WitIdentity, so
**both are licensed from the first stage.** WitForms, WitAnalytics and any later
service keep licensing as an available option, not an obligation — which is
precisely what §7.4 is for.

**One licence per instance, one fingerprint per instance, N documents for a
stack of N services.** Each service generates its own `installId`, shows its own
fingerprint on its own licence page, produces its own request, and receives its
own document. Two services, two keys pasted — the same way a second WitCloud
instance means a second key.

An earlier draft of this section argued for a shared deployment identity so that
a stack would present *one* fingerprint. **That was solving a problem that does
not exist.** Per-instance licensing is the normal shape for node-locked on-prem
software, it is what the purchase actually is, and pasting a second key into a
second admin page is not friction anybody has ever complained about. It also
kept a schema change on the pre-deployment list that is now not needed (§7.8.2).

Two findings from that draft survive, and both are real.

#### 7.8.1 The `tenant` factor has no source — and 1-of-2 does not stop the threat it was built for

**First: the factor cannot be read.** [`DESIGN.md`](DESIGN.md) §7.2 defines the
server binding as `tenant` + `installId`, where `tenant` is "the slug the
installer already writes into `tenant.json`". It does not. **Neither
`tenant.json.example` contains a slug** — WitCloud's carries
`Hosting.PublicBaseUrl` and `Branding.AppName`, WitIdentity's the same shape.
The slug lives in the *installer's* templates, where it names the output zip
`omnibuscloud-<slug>-<version>.zip`. So `tenant` as specified has nowhere to
read from, and something has to give.

**Second, and more interesting: at 1-of-2, `tenant` is what makes a licence
portable.** With threshold 1, *either* factor alone satisfies the binding. So a
licence issued to `acme-gmbh` validates on **any** deployment whose config says
`acme-gmbh` — including the second instance the same customer stands up next
week, because cloning a deployment means copying `tenant.json`.

That is not a hypothetical. It is **failure mode #1** in
[`DESIGN.md`](DESIGN.md) §3.1: *"a second instance gets stood up 'just for
testing'"*. The binding designed to make that visible currently permits it
silently. §7.2 of that document claims a copied licence "satisfies neither" in
another deployment — true for a *different customer*, but the realistic case is
the same customer, same slug, second box.

The options, and what each actually costs:

| Binding | Fresh second instance | Volume copied to another host | Volume lost / rebuilt | Renamed / new domain | Needs a tenant source |
|---|---|---|---|---|---|
| `tenant` + `installId`, **1 of 2** (as designed) | **Accepted** | Accepted | Works | Works | Yes |
| `installId` only, **1 of 1** | Refused | Accepted | Transfer | Works | **No** |
| **`installId` + `publicBaseUrl` + `issuer`, n-of-n** | Refused | **Refused** | Transfer | Transfer | **No** |

**Recommended: the third row, at n-of-n** — for the reason set out in §7.8.2 and
with the factor list settled in §7.8.3: it is the only option that reaches the
copied-volume case at all, and the factors it adds are the only ones available
that cannot be faked without breaking the deployment.

If the domain-migration cost is judged too high, the second row is the fallback
and still fixes failure mode #1. What is **not** defensible is the first row: it
is the shipped design, and it permits the very case the binding exists to catch.

`tenant` does not vanish under either — it stops being an *authorisation* factor
and stays a *record*. The customer block and `notes` already carry the
human-meaningful identity, and that is where the admin panel reads it from
anyway. `LicenseBindingProviderTenant` takes an arbitrary factor dictionary, so a
customer who genuinely needs slug-tolerance can still be issued at 1-of-2 as a
deliberate exception with the reason recorded.

The cost is honest and small: a rebuilt stack that lost its volume, or one that
moved to a new domain, needs a **Transfer** — a V1 capability in
`WitLicense/DESIGN.md` §5.6 with a soft cap of 3 per year. §10 of that document
already names the diagnostic — *if transfers are frequent, the binding factors
are wrong* — so the support volume is the signal to watch rather than a surprise.

#### 7.8.2 Cloning the volume onto ten machines

The obvious attack, and it deserves a straight answer: **yes, completely.**
`docker compose down`, tar the licence volume, ship it, `up` on ten hosts — all
ten validate, forever, and nothing in an offline scheme can prevent it.
[`DESIGN.md`](DESIGN.md) §3.2 says so ("a copied deployment volume clones the
server identity — mitigated by contract, not by code") and §7.2 repeats it. That
is not a gap; it is a term of the design.

But the threat model (§3.1) ranks four failure modes, and the binding is only
supposed to make **(1) impossible** and **(2)–(3) deliberate and visible**. It is
worth checking which of them each option actually delivers, because they are not
the same:

| | Stand up a second instance the normal way | Copy the identity file to ten hosts |
|---|---|---|
| Failure mode | **(1) accidental** — *"a second instance gets stood up just for testing"* | **(3) deliberate** |
| `tenant` + `installId`, 1-of-2 | **Accepted.** The slug is in the config that gets copied | Accepted |
| `installId` only | **Refused** — fresh volume, fresh identity | Accepted |
| `installId` + `publicBaseUrl` (+ `issuer`), n-of-n | **Refused** | **Refused, unless the copies are useless** — see below |

The first row is the one that matters most, and it is the argument for §7.8.1:
the shipped design does not currently catch failure mode #1, which is the one it
was written to make impossible.

##### The one factor that reaches the clone case

A cloned WitCloud that is *actually useful* must be reachable, and reachable at a
**different address** — its worker clients connect to it, its OIDC redirect URIs
point at it, its emails link to it. That address is already in both
`tenant.json` files as `Hosting.PublicBaseUrl`, and it has a property no other
available factor has:

> **It cannot be faked without breaking the deployment.** Setting it to the
> licensed value on a host serving a different address breaks worker
> registration, OIDC redirects and every emitted link — the clone stops being
> worth having. Every other factor can be lied about for free.

Adding it as a factor that must also match gives:

| Event | Outcome |
|---|---|
| Container recreated, upgraded, migrated to new hardware | Both factors stable → **works** |
| Load-balanced replicas of one deployment (same URL, shared volume) | Both match → **works**, correctly treated as one deployment |
| Fresh second instance | Neither matches → **refused** |
| **Volume copied to another host with its own address** | installId matches, URL does not → **refused** |
| Customer migrates to a new domain | **Refused → Transfer.** The real cost |
| Staging / blue-green on a separate URL | Needs its own licence. Cheap to issue, arguably correct |

That is the only proposal on the table that touches the question asked, and it
costs one config read. Its price is honest: a domain migration becomes a
Transfer, and a customer who runs staging on a different hostname needs a second
document. Both are ordinary operator events with an existing V1 flow behind them
(`WitLicense/DESIGN.md` §5.6).

It does **not** make cloning impossible — ten copies all claiming one address
still validate, they are simply not useful as ten independent deployments. That
is the whole of what an offline binding can do: not prevent, but ensure the
cheat costs the cheater something.

##### What actually bounds the deliberate case

Unchanged from [`DESIGN.md`](DESIGN.md) §3.3, and worth restating because no
binding replaces it:

- **Term length.** A cloned fleet expires on one date. Every renewal is a
  conversation that has to happen ten times or not at all.
- **`checkIn`** (stage 4) is the only real technical answer, and it reaches this
  case for free: a licence configured to check in every 7 days that checks in ten
  times a week has ten instances. That is a **registry-side report for an account
  manager**, not an enforcement lever — NAT, failover and blue-green produce the
  same signal — and it needs no format change, because the field is already
  reserved.
- **The `jti` is visible in every licence panel and every log line**, so a
  support ticket from an instance whose `jti` does not match the caller's record
  is a detection channel that costs nothing to have.
- **The commercial reality**: what is sold is updates, the controller catalogue,
  support and the fleet. Ten copies of the bits without any of that is a worse
  product than one licensed instance.

#### 7.8.3 How many factors, and which — the selection rule

The instinct is right and worth making into a rule, because "add more factors"
without one produces the legacy library's failure (§7.3 of
[`DESIGN.md`](DESIGN.md): everything hashed into one string, and a RAM upgrade
killed the licence).

> **A factor earns its place only if lying about it breaks the deployment.**
> Everything else is a way for a paying customer to lose a working system.

By that test, here is the whole candidate list for a service, honestly scored:

| Candidate | Lie about it and… | Legitimate change | Verdict |
|---|---|---|---|
| **`installId`** | nothing breaks — but it is unguessable and must be *stolen*, not invented | never | **Take.** The anchor |
| **`publicBaseUrl`** (`Hosting.PublicBaseUrl`) | worker registration, OIDC redirect URIs and every emitted link break | domain migration | **Take.** The one that reaches the clone |
| **`issuer`** (`Identity.Issuer`, WitCloud only) | **token validation fails cryptographically** — the `iss` claim is checked against the discovery document | identity domain migration | **Take.** Strongest of the three: enforced by the running system, not by convention |
| `Branding.AppName` | nothing | rebrand | Reject — cosmetic |
| Admin UI client id | admin login breaks | rare | Reject — usually a constant, not per-deployment |
| CORS origins, email `From` | little, or nothing | occasionally | Reject — derived from the URL, or shared across deployments |
| First admin user id | nothing | staff turnover | Reject as a factor. Keep as an audit record |
| TLS certificate | serving breaks | every 60 days with Let's Encrypt | Reject — the stable part *is* the domain |

Three, at **3-of-3**. Deployments do not drift, so an all-must-match threshold is
the correct shape here — the opposite of the workstation's 2-of-3, and for the
opposite reason (§4.1).

**And the list stops at three**, deliberately. Beyond that, each extra factor
multiplies the ways a paying customer's deployment dies while closing no new
clone scenario: if a clone is already refused because its URL differs, refusing
it a second time because its issuer differs has bought nothing and added a
support event. Redundant strength is not free.

##### Decided: `installId` comes from `.env`, generated at deploy

An earlier draft of this section argued for the database, on the grounds that
copying a database is a heavier act than copying a file. **That was the wrong
trade, and the reasoning was thin.** A cloner who wants a working deployment
copies the whole deployment directory anyway — including the database — so the
"heaviness" bought little. Meanwhile the database cost a table, a migration on a
provider whose migration story is already fragile (`WitLicense/DESIGN.md` §9.1),
an async store API, deferred evaluation and a whole new licence state.

Configuration is better on every axis that turned out to matter:

| | `.env` (decided) | Database | Volume file |
|---|---|---|---|
| Available at composition time | **Yes** — licensing evaluates eagerly, as it does for clients | No — after migrations | Yes |
| Replicas of one deployment agree | **Yes**, by construction | Yes, after a race | No |
| Knowable before first start | **Yes** — request a licence while installing | No | No |
| Backed up | With `.env`, which already holds the database password | With the data | Only if someone remembers |
| Library cost | **None** | A table, a migration, async store, a new state | A file store |

The security difference is marginal, because **`installId` was never the
clone-catcher**. Its job is only "a fresh deployment is a different deployment",
and a fresh deployment gets a fresh `.env` from the installer. The work of
refusing a *copied* deployment is done by `publicBaseUrl` and `issuer`, which
are configuration too.

The one real risk is an upgrade that regenerates `.env` from a template. That
discipline already exists and is already load-bearing — the same file holds the
database password — so this adds a key to a file operators already know they
must not lose, rather than creating a new thing to protect.

**This takes gaps 9 and 10 off §5 entirely**, and reduces §9.8 from a redesign
to a paragraph: with identity in configuration and tokens in the env var and
file store [`DESIGN.md`](DESIGN.md) §13 already specifies, licensing touches no
database at all and the eager-evaluation model survives unchanged for both
families.

##### Two traps in hashing a URL

`FactorHasher.Normalize` is trim plus invariant lower-case — right for host
names, **not sufficient for URLs**:

- `https://cloud.acme.com` and `https://cloud.acme.com/` hash differently. Two
  configurations a human would call identical produce a `BindingMismatch` on a
  perfectly good deployment.
- `https://cloud.acme.com:443` and `https://cloud.acme.com` likewise.

Both factors added in this section are URLs, so URL normalisation — scheme, host,
explicit non-default port, no trailing slash, no path — has to happen **before**
hashing, on both the issuing side and the verifying side, from one shared
function. This is precisely the class of defect that costs a day at a customer
and five minutes to prevent.

##### Factors can be added later without invalidating anything

Worth stating because it de-risks this whole discussion: the **threshold travels
in the payload**, and `LicenseBindingMatcher` ignores recorded factors the host
cannot produce and host factors the licence did not record. So a licence issued
today against two factors keeps validating against a build that later presents
three, and a build shipped today keeps honouring a licence issued later against
more.

Nothing here is a one-way door. If `issuer` turns out to cause more transfers
than it prevents copies, it can be dropped from new issues without touching a
single installed licence.

##### The scenario none of this closes

Two identical deployments on **isolated networks with the same configuration**
are genuinely indistinguishable offline. Same URL, same issuer, copied database
— every factor matches, because from the product's point of view nothing is
different. And that is not an exotic case: it is the air-gapped CAE customer,
which is the market on-prem is *sold to*.

So the honest summary of what factors achieve:

| Clone shape | Caught by |
|---|---|
| Fresh second instance | `installId` |
| Volume copied, new address | `publicBaseUrl` |
| Volume copied, pointed at a different identity server | `issuer` |
| **Whole stack cloned, same config, isolated network** | **Nothing offline. Only `checkIn` (stage 4), and only if the customer is not air-gapped** |

The last row is why [`DESIGN.md`](DESIGN.md) §3.3 puts the real defence in the
contract and in the service being the valuable part. Factors make the accidental
case impossible and the casual case pointless; they do not make the determined
case impossible, and a design that claimed otherwise would be lying.

##### Show the factors in the panel

Nearly free, and it converts the commonest support call into a glance. The
licence records hashes, so the panel cannot print what a licence was bound *to* —
but the product knows its own current values, so it can show which ones match:

```
Bound to     installId      ✓
             publicBaseUrl  ✗   this instance: https://cloud-2.acme.com
             issuer         ✓
```

*"Your licence was issued for a different address"* in one line, at the moment
the operator is looking for it, instead of `BindingMismatch` and an email.

#### 7.8.4 N requests, N documents — and no schema change

Each instance produces its own request blob and receives its own document. The
earlier draft's "one blob → N documents" would have required
`LicenseRequest.linkedLicenseJti` to become a collection — a schema change, on a
provider where `WitLicense/DESIGN.md` §9.1 is explicit that schema changes after
deployment are not free. **Per-instance requests need none of it**, and batch
issue goes back to where that document parked it: worth building if multi-seat
becomes the common case, not before.

What stays ruled out is a **single multi-product token** (`products[]` instead of
`product`): a format change, it would break the hard product check
`LicenseValidator` performs first, and it would couple the renewal cadence of
every service in the stack. Separate documents cost one paste each and keep every
service independently renewable.

> **Correction owed to [`DESIGN.md`](DESIGN.md) §4:** that document's topology
> diagram says on-prem WitIdentity is "unchanged; knows nothing of it", and §4
> repeats "WitIdentity itself is NOT MODIFIED". That remains true of the
> *issuing* side — WitLicense is still a standalone service and WitIdentity is
> still only an OIDC client of it. It is no longer true of the verifying side,
> and `DESIGN.md` should be amended rather than left to contradict this.

---

## 8. WitSweep integration

### 8.1 Shape of the work

| Aspect | Value |
|---|---|
| Product key | `WitSweep` · fingerprint prefix `WSW` |
| Binding | `LicenseBindingProviderMachine`, threshold 2 (fallback 1 — §4.2) |
| Demo | 30 days, core features only, `maxVariants` capped |
| Renewal grace | 0 days (§3.1) |
| Store | `LicenseStoreFile` under `IStandardDirectoryProvider`'s per-user config directory |
| Key ring | `witsweep.keyring.json`, embedded; the development ring embedded in Debug only |
| `appVer` | From `SweepVersionInfo` |
| Failure surface | Dialog on the attempted Run **plus** a persistent banner |

### 8.2 The five touch points

1. **`App.axaml.cs`** — construct the service beside `SweepSettingsStore` and
   `SweepSessionService`. WitSweep has no `IServiceCollection`, so this needs
   library gap 8. It must not block the window: `AddLicensing` performs a
   synchronous `ReloadAsync` at startup, which is safe only because every
   `await` in the package uses `ConfigureAwait(false)` — the deadlock the
   harness already found once (`SynchronizationContextTests`). Verify, do not
   assume.
2. **`ApplicationViewModel`** — one more injected service, `ILicenseService`,
   beside `Session`. Container only; no logic.
3. **`ShellViewModel.UpdateStatus()`** — `CanRun` gains
   `&& ApplicationVm.Licensing.Mode is Licensed or Demo or Grace`, right beside
   the existing `Session.IsSignedIn`. The two conditions sit adjacent and are
   deliberately *not* merged.
4. **`ShellViewModel.StartRun()`** — the honest refusal. Today `CanRun == false`
   merely disables the button and says nothing; a licence refusal must produce
   text, in the existing `HonestyText` idiom, distinct from the sign-in one.
   This is where §2.1 is either honoured or lost.
5. **The Settings screen and its Licence section** — see §8.3, because it is
   larger than it sounds.

### 8.3 WitSweep has no Settings screen at all

Worth stating plainly, because the request assumed otherwise: **there is no
settings UI in WitSweep today.** `SweepSettings` exists as a model over
`OutWit.Common.Settings`, with `[Setting("Connection")]` categories for
`ServerUrl`, `IdentityUrl`, `ApiKey`, `ConnectTimeoutSeconds`, `Theme`,
`LogLevel` — and **none of it is surfaced anywhere**. `App.axaml.cs` reads
`Theme` and `LogLevel` at startup; the rest is edited by hand in
`witsweep.json`.

So "a Licence section in Settings" means building Settings. Two ways to take
that, and the choice belongs to the user:

| | **A — Settings screen with sections** *(recommended)* | **B — Licence-only screen** |
|---|---|---|
| Scope | A stage in the existing rail (the VD-5 sidebar idiom `MainWindow.axaml` already names), with sections: Connection, Appearance, Logging, **Licence** | One stage, licence only |
| Extra cost | ~1 day — the non-licence sections are a form over settings that already exist and already validate | none |
| In three months | Done | Becomes A anyway, and the rail entry gets renamed under a user's feet |
| Risk | Slightly wider than the licensing task | Ships a screen called "Licence" that everyone will try to change the server URL in |

**Decided: A** — with the non-licence sections built thin, since the settings
model already carries the schema, the categories and the defaults, so the form
is mostly mechanical. It was flagged rather than absorbed silently because it is
scope the licensing work does not strictly require, and it should stay visible
in the estimate.

The Licence section itself is the shared `LicensePanelViewModel` (§7) behind an
Avalonia view, with `IClipboardService` and `IFilePickerService` adapted to the
two seams. Reachable in **every** mode, including `Restricted` — that is §2.2.

### 8.4 What must *not* happen

- No licence check anywhere near `SweepSessionService`, `RestoreAndReattachAsync`
  or the OIDC flow. Sign-in must work with no licence and a licence must work
  with no sign-in.
- No gate on `SweepPriorJobLoader`, the results grid, or any export path (§2.2).
- No gate on reattach. A sweep already submitted is running on the server; the
  workstation licence has nothing to say about watching it finish.

---

## 9. The service family — WitCloud, and WitIdentity beside it

### 9.1 Shape of the work

| Aspect | Value |
|---|---|
| Product keys | `WitCloud` (`WCL`) and `WitIdentity` (`WID`) — **one licence each, one fingerprint each** (§7.8) |
| Binding | `LicenseBindingProviderTenant` over `installId`, `publicBaseUrl`, `issuer` — threshold **3 of 3** (§7.8.3). WitIdentity drops `issuer`, which for it is its own URL: **2 of 2** |
| `installId` | 128 random bits, generated at deploy into `.env` as `Licensing__InstallId` (§7.8.3); a volume file is the fallback when unset (§4.3) |
| `publicBaseUrl` | `Hosting.PublicBaseUrl` from `tenant.json` — a clone cannot fake it and stay reachable (§7.8.2) |
| `issuer` | `Identity.Issuer` — a clone cannot fake it and still validate a token (§7.8.3) |
| `tenant` | **Not a binding factor.** Recorded in the licence's customer block and notes, where the admin panel reads it anyway |
| Demo | 30 days, `maxNodes: 2`, `maxConcurrentJobs: 2`, core features |
| Renewal grace | 14 days (§3.1) |
| Store | `LicenseStoreComposite`: `Licensing__License` env var **and** `/app/license/*.lic` (gap 4). No database involvement anywhere in licensing (§9.8) |
| Key ring | `witcloud.keyring.json`, embedded; dev ring in Debug only |
| Failure surface | Admin UI banner + `/health` field + `Warning` log + a licensing-specific reason on the refused call |

### 9.2 The enforcement points

| Point | Site | Behaviour |
|---|---|---|
| **New node registration** | `Channels/RegistrationChannel.RegisterClient` | `Restricted` → refuse with a licensing reason. Over `maxNodes` → refuse the *new* node only; registered nodes are never evicted |
| **New job intake** | `Channels/ApiChannel.SubmitAsync` | `Restricted` → refuse. Running and queued jobs are untouched |
| **Concurrency cap** | `Services/ProcessingSchedulerService` | Over `maxConcurrentJobs` → **queue**, never reject. A cap is a throttle, not an error |
| **Feature guards** | Channel guards, beside the existing `EnsureAdmin()` | `Result.Unauthorized()` with a licensing reason distinct from the authorisation one |
| **Visibility** | `HealthChecks/` + a licence panel in `OutWit.Cloud.UI` | Always visible, never silent |

Where a cap needs a live count, it reads the existing managers
(`WitNodesManager`, `ClientPoolManager`, `ProcessingManager`). Licensing sets
caps; accounting measures usage; neither reimplements the other
([`DESIGN.md`](DESIGN.md) §10.3).

### 9.3 The Licence page — almost all of it comes from the package

Because §7.4 puts the channel, the DTOs and the MudBlazor panel in
`OutWit.Shared.Licensing.*`, WitCloud is the **first consumer** of that family
rather than the author of a bespoke surface. What it actually writes:

| Piece | Where | Size |
|---|---|---|
| `LicenseChannel : LicenseChannelBase` | `OutWit.Cloud/Channels` | ~10 lines — supplies `EnsureOperator<T>()` over the existing `PrincipalStore` + `Constants.Roles.Admin` |
| `srv.AddService<LicenseChannel>()` | `Startup.cs` | 1 line, beside the other fifteen |
| `services.AddServiceLicensing(...)` | `Startup.cs` | 5 lines — product, tenant, installId path, grace |
| `Views/Pages/License.razor` | `OutWit.Cloud.UI` | 5 lines — route, `@inherits LicensePageViewModel`, `[Authorize]`, `<LicensePanel />` |
| Nav entry + `<LicenseBanner />` in `MainLayout` | `OutWit.Cloud.UI` | 2 lines |
| The `installId` volume + compose mount | `Dockerfile` / `docker-compose.yml` | §4.3 — the only genuinely new infrastructure |
| The enforcement points | §9.2 | The real work, and correctly per-service |

`ILicenseChannel`, `LicenseInfo`, `LicenseGatewayChannel`, `LicensePanel`,
`LicenseBanner` and `LicensePageViewModel` are **not written here at all** — they
arrive as packages, and WitIdentity / WitForms / WitAnalytics get them for the
same five lines each. The MemoryPack DTOs live in the contracts package and map
to `LicenseSnapshot` inside the gateway, which keeps MemoryPack out of
`OutWit.Common.Licensing` — that package has no business knowing about a
transport.

Three decisions worth making explicitly:

- **Install is admin-only; so is the request blob.** The blob carries factor
  hashes. Not secret (they are hashes, and the licence itself is readable by
  design) but it is operator business, and the operator is an admin.
- **No live event over the wire in this phase.** The page refreshes on open and
  after each action, which is what `AutoRefreshViewModel` already does for every
  other page. The `StateChanged` event from gap 1 is an in-process affordance for
  the host, not a push channel to the browser.
- **The persistent banner needs a cheaper source than the page.** A separate
  `GetSummary()` returning mode + expiry + days remaining — small enough to poll
  from `MainLayout` on a slow timer without pulling the whole snapshot on every
  tick.

`/health` exposes **mode and expiry only** — no customer, no `jti`, no features.
It is unauthenticated, and the operational question it answers is "is this
deployment about to stop taking work", not "what did they buy".

### 9.4 Reconnect is not registration

`RegistrationChannel` has two entry points — `RegisterClient` and `Reconnect` —
and only the first is a licensing decision. A node that was admitted under the
licence and drops its WebSocket must be able to come back without re-passing a
cap it is already counted in; otherwise a network blip mid-shift silently
shrinks the pool. Gate `RegisterClient`; leave `Reconnect` alone.

### 9.5 Our own hosted instance

`engine.omnibuscloud.com` is ours, so [`DESIGN.md`](DESIGN.md) §7.1 gives it
`kind: none`. It should nevertheless **carry a real, unlimited, `kind: none`
licence** rather than a bypass flag, for the same reason demo is a real licence:
one code path. A build with an "unlicensed mode" branch is a build where that
branch is the crack.

### 9.6 Applying a licence without a restart

[`DESIGN.md`](DESIGN.md) §8.1 requires it and it falls out of gaps 1–2: the
panel calls `InstallAsync`, the service re-evaluates and raises `StateChanged`,
the enforcement points read `State` per call rather than caching it at startup.
The rule for reviewers is simply **never cache `CanRun` in a field**.

### 9.7 WitIdentity — the one service where a wrong gate locks everyone out

WitIdentity is licensed from the first stage (§7.8), and it is the service where
§2.2 stops being a principle and becomes a hard safety requirement.

**The failure mode to design against, stated first:** WitIdentity issues the
tokens that every admin UI in the deployment authenticates with — including
WitCloud's licence page, and including WitIdentity's own. If a lapsed licence
could refuse authentication, the operator would be **locked out of the two
screens where the fixing licence gets pasted**, permanently, with no path back
that does not involve editing files on the host. That is not a degraded product;
it is a bricked deployment, and it would be caused by the thing meant to prevent
one.

So the levels resolve unusually, and the level-0 list is longer than anywhere
else in this document:

| Level | WitIdentity |
|---|---|
| **0 — never gated** | **All authentication**: `/authorize`, `/token`, refresh, passkey, external providers, OIDC discovery, JWKS. Sign-in UI. Admin UI. User self-service and profile. The licence page. `/health`. Every existing user, client and API key keeps working |
| **1 — entitlement** | Creating a **new user account**; registering a **new OIDC client** or API key. Growth actions, all of them |
| **2 — attributes** | `maxUsers` (active accounts), `maxClients` (registered applications); features such as external federation or SCIM, whichever the catalogue ends up naming |

Three consequences worth writing down:

- **Caps are enforced at creation, never by disabling.** Over `maxUsers`, the
  *next* account is refused; not one existing account is deactivated. Same rule
  as `maxNodes` in §9.2, and for the same reason — evicting what already works is
  how licensing gets ripped out of a product rather than renewed.
- **A `Restricted` WitIdentity is a fully working identity provider that has
  stopped growing.** Everybody signs in, nothing new gets created, the banner
  says why. That is precisely the "degrade, never stop" commitment of
  [`DESIGN.md`](DESIGN.md) §2, applied to the one service that cannot afford to
  be wrong about it.
- **This does not weaken §2/§5 of [`DESIGN.md`](DESIGN.md).** WitIdentity is
  still the answer to "who are you"; its *licence* still answers only "may this
  deployment run this software". The two axes stay orthogonal — what is new is
  merely that the identity server is itself a licensed product, which says
  nothing about any user.

Everything else is the five lines of §9.3: `AddServiceLicensing`, a
`LicenseChannel` subclass, the page, the nav entry and the banner. WitIdentity
is the **second consumer** of
`OutWit.Shared.Licensing.*` and therefore the real test of §7.1 — if it costs
more than that, the seam is wrong and should be corrected before a third service
copies it.

### 9.8 When a service evaluates

`AddLicensing` evaluates **synchronously during service registration**
(`ServiceCollectionExtensions.cs:51`), and the comment there defends it well: a
product that discovers its licence state on first use discovers it at an
arbitrary moment, and a startup banner rendering before the state settles is a
bug nobody reproduces reliably.

Because §7.8.3 puts identity in configuration and [`DESIGN.md`](DESIGN.md) §13
puts tokens in an env var and a file, **that model survives unchanged for
services**: everything licensing reads is available at `ConfigureServices` time,
before the database plugin is loaded and long before
`StartupService.ExecuteAsync` applies migrations. One evaluation model, both
families, no new licence state.

This is worth recording because it was nearly not so. Holding identity in the
database would have made evaluation impossible at composition time — WitCloud's
database arrives through a provider plugin (`Startup.cs:159`) and migrates
inside a `BackgroundService` — and would have forced a deferred mode plus a
*not-evaluated-yet* state distinct from `Missing`, since `Missing` starts a demo
and a service that answered it while its database opened would self-issue a demo
on every start. None of that is needed now, and the reason it is not needed is
that licensing touches no database.

The one rule that remains: **enforcement points read `State` per call and never
cache `CanRun` in a field** (§9.6), so a licence installed at 03:00 takes effect
without a restart.

---

## 10. Work this lands on the issuing side

Tracked separately, as asked. None of it is large; most of it is the first real
use of something already built.

**Blocking — needed before a single licence can be issued to a product:**

1. **Catalogue entries for `WitCloud`, `WitIdentity`, `WitSweep` and the Inventor
   add-in** — feature and limit vocabularies, binding kinds, version ranges. The
   service ships empty by design (`WitLicense/DESIGN.md` §2), so nothing can be
   issued until these exist. They must agree exactly with what each product
   passes to `Declares(...)`, or the customer silently does not get what they
   bought (§6 of that document names this hazard) — which is what §11.8 proposes
   to fix at the root rather than by proof-reading.

   **The add-in is a fourth product, not a WitSweep feature.** It gets its own
   catalogue entry, its own key ring and its own licence, and buying WitSweep
   does not convey it. If that is ever meant to change, the place to decide it is
   the price list, before the first licence is issued — a licence already in a
   customer's hands is the most expensive place to discover that two products
   were meant to be one.
2. **Key ring export, exercised** — generate the production and development keys
   for all four product lines, export `witcloud.keyring.json`,
   `witidentity.keyring.json`, `witsweep.keyring.json` and the add-in's, and
   consume one in the harness (§6.1) before any product embeds it.
3. **Short-term issue presets** — **1 hour, 1 day, 7 days**, alongside the
   existing durations. An hour-scale term is what makes expiry, grace and
   renewal overlap testable in real time instead of by clock travel; clock
   travel proves the branch, a real short licence proves the wiring.
4. **`appVer` default and its one hard rule.** Decided (§3.4 covers the product
   side; this is the issuing side):
   - The form **prefills `>={current} <{next major}`** and stays editable. That
     makes *"perpetual for the version you bought, a new document for a major
     upgrade"* — the shape `DESIGN.md` §6.2.1 already recommends — the default,
     without anyone deciding it per deal.
   - **`Unlimited` term together with an unbounded range is refused.** It is the
     one grant that is irreversible in both dimensions at once: no expiry, no
     revocation, and every future major version free forever. Overriding it must
     take an explicit action with a typed reason that lands in the audit log, not
     a checkbox.
5. **Range validation on the form** — because the product **fails open** on a
   malformed range (§3.4): an unparseable clause is silently dropped and an
   unparseable range matches everything. `">=1.5.0 <2.x"` quietly becomes
   `">=1.5.0"`. The form must parse each clause, refuse to sign an unparseable
   one, and **preview which versions the range covers** before signing.

**Needed during the phase:**

6. **Binding kind and threshold on the issue form** — the payload carries both;
   the form must let an operator issue an instance binding at 1-of-1, drop a
   laptop to 1 factor when fewer than three are usable (§4.2), or grant a
   deliberate 1-of-2 slug-tolerant server licence as an exception (§7.8.1) — each
   with the reason recorded.
7. **`.owlreq` import against a real blob** — the format has only ever been
   round-tripped inside the harness. The first blob from an actual product build
   is the real test of the import path.
8. **Email delivery, actually exercised** — Resend has never sent a message.
   The first test licence goes out by email, not by download.
9. **v1.0.5 rolled out** via `witlicense-update` in Cockpit — pending, and the
   version that will issue the first real licences.

**Deferred, but on the list:**

10. **Descriptor import** (`WitLicense/DESIGN.md` §6, V2) — reading the
    `<product>.product.json` that §11.8 makes the product emit. Until then the
    catalogue is typed by hand and item 1 is proof-read by eye.
11. **`OutWit.License.Fingerprint`** — the standalone utility. Only needed when a
    customer cannot start the product at all; every product shows its own
    fingerprint in-app, which is always better. Not a blocker.
12. **`checkIn` on the issue form** — stage 4.
13. **Batch issue** — back where `WitLicense/DESIGN.md` §5.5 parked it. Per-instance
    requests removed the reason to promote it (§7.8.2); it returns to the list only
    if multi-seat desktop sales become the common case.

**Removed from this list since the previous draft:** the multi-licence request
(`LicenseRequest.linkedLicenseJti` becoming a collection). Per-instance
licensing means one request produces one licence, so the schema stands as
deployed — which matters, because `WitLicense/DESIGN.md` §9.1 makes any
post-deployment schema change expensive on this provider.

---

## 11. Aspects, obfuscation, source generation — where each earns its place

Three tools, three different answers, and the differences are the point:

| Tool | Verdict |
|---|---|
| **Aspects for the gate** | **No** — for anti-tamper, never; for ergonomics, not at this count, and only ever in the service family (§11.1–§11.6) |
| **Obfuscating the checks** | **No** — a logic attack is not defeated by hiding logic (§11.1) |
| **Obfuscating the key ring** | **Yes** — a data attack *is* defeated by hiding a string, and it is the cheaper attack (§11.7) |
| **Source generation** | **Yes, three places** — and one of them removes a hazard nothing else catches (§11.8) |

### The proposal

A `[RequiresLicense]` attribute in the shape of
`OutWit.Common.Logging`'s `[Log]` / `[NoLog]` — AspectInjector, `Scope.Global`,
`Kind.Around` on `Target.Method`, class-level application cascading to every
method, method-level for a single one. The hoped-for benefits: less visible
call-site code, and checks scattered densely enough that decompiling and
stripping them becomes impractical, especially behind Eazfuscator.

**Recommendation: no.** Not as a first cut, and probably not later. The
reasoning, in the order that decided it:

### 11.1 It does not raise the cost of cracking

This is the load-bearing objection, and it is worth being exact about.

Every enforcement point in either product — hand-written or woven — reduces to
reading one property, which reduces to one method:
`LicenseValidator.Validate` → `LicenseValidationResult` → `LicenseState.CanRun`.
An attacker does not hunt for `if` statements; they patch the funnel. Six call
sites and six hundred call sites are cracked by the same one-line patch, because
they all ask the same question of the same object.

Worse, weaving makes the funnel *easier* to find, not harder. A woven aspect
emits a **uniform IL signature** at every site — the same call into the same
advice method with the same argument shape. That is precisely the pattern a
script matches and NOPs out in bulk. Hand-written checks are irregular, and
irregularity is the only property that resists automated stripping. The
intuition that "more checks are harder to remove" is inverted here: *more
identical* checks are easier to remove than *fewer varied* ones.

And the premise in the question is correct: with a decompiler and an LLM, either
shape comes out in an afternoon. [`DESIGN.md`](DESIGN.md) §3.1 already ranks
cracking last among realistic failure modes and §3.2 already commits to saying
so honestly rather than pretending otherwise. Building machinery that does not
move that needle would contradict a decision the design already made
deliberately.

### 11.2 An aspect cannot produce the thing that makes this design good

The best property of this licensing system is that a refusal **explains itself**:
`LicenseStatus` has eleven arms, every failure carries the parsed payload, and
`Describe()` produces a sentence a support engineer can act on
([`DESIGN.md`](DESIGN.md) §6.4). §2.1 makes this a rule.

A woven check can do exactly two things at a failing site: throw, or return
`default`. Throwing turns every gated call into a try/catch and surfaces an
exception where a sentence belongs. Returning `default` makes a method that
returns `Result` silently return `null`. Neither can produce
`Result.Rejected(licensing.State.Describe())`, and neither can participate in
the `UpdateStatus()` pattern that both UIs are built on — a disabled button is
not an exception.

The aspect would optimise the writing of the check and destroy the value of the
answer.

### 11.3 The economics are wrong — there are six sites, not six hundred

`[Log]` earns its weaving because logging genuinely belongs on every method.
Counting the enforcement points in this document: WitSweep has **two**
(`UpdateStatus`, `StartRun`); WitCloud has **four**
(`RegisterClient`, `SubmitAsync`, the scheduler cap, feature guards). Six sites,
each with a different failure shape, each deserving a hand-written sentence.

That is not an aspect-shaped problem. It is a problem where the correct amount
of visible code is *exactly the amount there is*, because a reviewer should be
able to read all six and check §2.1 at each.

### 11.4 It would require a static service locator

`LogAspect` works because Serilog's `Log.Logger` is a global. `ILicenseService`
is not: it is DI-resolved, and `LicensingOptions` deliberately admits no
ambient state so that the rules deciding whether a customer can work are
testable without a machine, a file or a wait (`LicenseValidator`'s class
comment).

An aspect would need `LicenseAmbient.Current`. That is a **mutable public static
that anything in the process can overwrite with a permissive stub** — a crack
that needs no IL patching at all, just a reflection call. It would also break
the harness, which runs several differently-configured services side by side.
The design would be strictly weaker than the one it replaced.

### 11.5 What to do instead, if hardening is genuinely wanted

Three things that do move the needle, in descending order of value per hour:

1. **Eazfuscator's own anti-tamper.** It is already in the toolchain
   (`OutWit.Cloud.Client.csproj`, `client-release.yml`), it is designed for
   exactly this, and it operates on the whole assembly rather than on call
   sites. Applying it to WitSweep is configuration, not code.
2. **Make the licence *data* load-bearing.** A check that is patched out is
   free; a *limit that the product actually computes with* is not. If
   `maxVariants` sizes the sweep expansion rather than merely being compared
   against it, a patched check yields a product that behaves wrongly rather than
   a product that is free. This is real, and it is also **risky** — the same
   mechanism misfires on a legitimate customer as a corrupted result rather than
   a clear refusal. Worth doing only where the failure is obviously a refusal.
   Not worth doing in the solver path.
3. **Vary the shape and the timing.** Where a redundant check is wanted, put it
   somewhere structurally different from the primary gate — at result assembly
   rather than at submission, on a different tick, written differently. Two
   dissimilar checks are worth more than twenty identical ones, for the same
   reason as §11.1.

### 11.6 Where the case for an aspect is stronger than stated above

Two of the objections above are weaker in the **service family** than the general
argument makes them sound, and it is worth correcting rather than leaving as
written:

- **§11.2 (an aspect cannot produce the message)** assumed the only outcomes were
  *throw* or *return default*. In a service that is not true: every channel
  method returns the house `Result<T>` / `ResultArray<T>`, which is uniform and
  generically constructible, so an aspect *could* emit
  `Result<T>.Failure(state.Describe())` with the real sentence intact.
- **§11.4 (it needs a static locator)** assumed no ambient resolution existed. In
  a service that is also not quite true: channels are `[InjectableHost]` with
  `[Inject]` properties, so an aspect could read an injected `ILicenseService` by
  convention — no public mutable static required.

What does **not** get weaker is §11.1: an aspect buys no anti-tamper value in
either family, and in the service family the point is entirely moot because
`OutWit.Cloud` is not obfuscated at all and should not be (§11.7.4). So for
services the question is **purely ergonomic, and purely about count**.

Today the count is six: four in WitCloud (§9.2), two in WitIdentity (§9.7). That
is not aspect-shaped. But it is a *revisit trigger with a condition* rather than
a closed door:

> If the third and fourth licensed services show the same uniform shape —
> `Result<T>` returns, `EnsureOperator` immediately followed by an entitlement
> check — then a `[RequiresLicense]` / `[RequiresFeature("key")]` pair in
> `OutWit.Shared.Licensing`, over `Result<T>` only, in the service family only,
> is defensible. Hand-write the first two so the real shape is known before it is
> abstracted.

One further constraint on that revisit, worth knowing before it is reached:
**`AspectInjector` is frozen at 2.8.2** (§12.3) — an upgrade has been tried and
it breaks. So any new aspect would be built on a weaver that cannot move. That
is survivable for `[Log]` and `[Notify]`, which are written and working, and it
is a poor foundation for something new: a weaving bug in a new aspect could not
be fixed by taking the fix upstream.

The same holds for `[Feature("key")]` on the client side if WitSweep's surface
ever grows to dozens of separately-licensed capabilities — the
`format.inp` / `format.nas` / `integration.prepomax` split in
[`DESIGN.md`](DESIGN.md) §6.2.2 is the seed of it. Today it is five.

**Decision: enforcement stays explicit, at the six named sites. Anti-tamper is
delegated to Eazfuscator and to the commercial argument in
[`DESIGN.md`](DESIGN.md) §3.3, which is what actually defends this product.**

### 11.7 Where the key ring lives — the one place hiding actually pays

The key ring holds **public** keys. Nothing about it is confidential, and
[`DESIGN.md`](DESIGN.md) §11 says so (Kerckhoffs — what is private is the vault
and the books, not the verifier). It would be easy to conclude that hiding it is
theatre, in the same way §11.1 concluded that scattering checks is theatre.

**That conclusion is wrong, and the reason is worth stating precisely.**

#### 11.7.1 Substitution is a better attack than removal

There are two ways past an offline verifier:

| | **Removal** — patch `CanRun` to return true | **Substitution** — replace the embedded public key with your own |
|---|---|---|
| Requires | Reading and editing control flow | Finding and replacing a string |
| Result | A patched binary with a broken invariant | A binary that **genuinely validates** — every check passes, honestly |
| Reapplying to the next release | Re-find the method | Re-run the same byte replacement |
| Scriptable | Partly | **Entirely** |
| Lets the attacker mint *arbitrary* licences | No | **Yes** — any customer, any term, any features, unlimited |

Substitution is strictly the better attack, and it is the one the design has so
far said the least about. It is also the only one where the defence is cheap:
removal requires *understanding* the binary, and nothing stops that; substitution
requires *locating a specific literal*, and that is exactly what string
encryption defeats.

So the instinct in the question is right — and it is right about the key ring
specifically, not about checks in general.

#### 11.7.2 The problem with an embedded resource

`WitLicense/DESIGN.md` §7.2 has the service export `<product>.keyring.json` and
the product embed it as an **embedded resource**. That export is correct and must
stay — it is what stops somebody copying PEM blocks by hand and eventually
copying the wrong one.

But an embedded resource is the **worst** landing place for it: a plain blob in
the assembly manifest, visible in any decompiler's resource view, findable with
`strings`, and replaceable without touching a single instruction. Crucially,
Eazfuscator's string encryption transforms **string literals in IL** — an
embedded resource is not a literal and is not covered by it. (Worth verifying
against the current Eazfuscator feature set before relying on the negative, but
plan for it.)

#### 11.7.3 The fix: generate the ring as `const string` at build time

Keep the export, change the landing:

```
witsweep.keyring.json          ← exported by WitLicense, committed, source of truth
        │  MSBuild target / source generator
        ▼
LicenseKeyRingEmbedded.g.cs    ← private const string RING = "{...}";
                                 public static ILicenseKeyRing Create()
                                     => LicenseKeyRing.FromJson(RING);
```

This gets both properties at once, and neither is sacrificed:

- **Mechanical export → embed.** No hand-copied PEM, so `WitLicense/DESIGN.md`
  §7.2's whole reason for existing is preserved.
- **A `const string` literal**, which is exactly what Eazfuscator encrypts —
  the shape the question already identified as working well.
- The development ring (`WitLicense/DESIGN.md` §7.3) generates under `#if DEBUG`
  from a second file, so a dev licence stays worthless against a Release build
  with no extra machinery.

Ranked honestly, what actually raises the cost:

1. **Const-string ring + Eazfuscator string encryption.** Cheap, mechanical, and
   it defends the *easiest* attack. Do it.
2. **Eazfuscator anti-tamper** (assembly checksum). Defends substitution *and*
   removal, operates on the whole assembly rather than on call sites, and is
   already licensed and wired for `OutWit.Cloud.Client`. Do it for WitSweep.
3. Everything else — a hash of the ring checked against another constant, a
   canary token, redundant rings — is theatre. An attacker who can edit one
   constant can edit two.

#### 11.7.4 Three limits to state plainly

- **This is tamper-resistance, not secrecy.** The ring stays readable with
  effort, and it should — a customer's security team is entitled to see what the
  product verifies against. The property bought is "not trivially swappable".
- **It does not apply to the server.** `OutWit.Cloud` is not obfuscated and
  should not be: it ships as a Docker image, and readable stack traces in
  production logs are worth far more operationally than the marginal difficulty
  they would buy. An on-prem server's key ring is replaceable, full stop — which
  is consistent, because §3.1 puts that customer in the "procurement, legal and a
  reputation" bracket where the contract does the work.
- **The ring never goes into `OutWit.Common.Licensing`.** It is per product line
  by construction ([`DESIGN.md`](DESIGN.md) §6.3.2); the package stays
  ring-agnostic and takes one through `WithKeyRing(...)`. The Blazor admin
  surface holds no ring at all — verification happens on the host, so the browser
  has nothing to substitute.

### 11.8 Where source generation earns its place

Three uses, in ascending order of value. The third is the one worth building
even if the other two were skipped.

**1. The key ring → `const string`** (§11.7.3). An incremental generator over
`AdditionalFiles`: `witsweep.keyring.json` in, `LicenseKeyRingEmbedded.g.cs`
out, dev ring under `#if DEBUG` from a second file. Already decided; noting here
that it *is* source generation, and that a generator is preferable to an MSBuild
target because it participates in IDE builds and design-time compilation rather
than surprising someone at `dotnet build`.

**2. `LicenseSnapshot` ↔ MemoryPack DTO mapping** (§7.4). Could be generated.
Should not be — it is about twenty lines, written once, in one place, and a
generator for it would be more code than it removes. Listed only so the question
is closed rather than reopened at review.

**3. The product vocabulary — one file, three outputs.** This is the valuable
one, because it kills two hazards that nothing currently catches.

Today the feature and limit keys exist in **three places that must agree and are
checked by nobody**:

| Place | Failure when it disagrees |
|---|---|
| The registry catalogue (typed by an operator) | The customer pays for `SSO`, the product checks `sso`, **nothing fails and the feature is simply off** (`WitLicense/DESIGN.md` §6) |
| `Declares(v => v.Feature("sso", …))` in the product | Only affects the unknown-key report |
| `HasFeature("sso")` at the call site | A typo here is a **silent, permanent false** — no compiler error, no warning, no unknown-key report, because the report only sees what the *licence* granted, never what the *code* asked for |

The third row is the one with no mitigation at all today, and it is the easiest
mistake to make: `HasFeature("ssoo")` compiles, runs, and quietly disables a
capability the customer paid for.

The fix is one file per product, committed in the product repo:

```jsonc
// witsweep.product.json — the single source of truth
{
  "product": "WitSweep",
  "features": [ { "key": "format.nas", "name": "Nastran decks" } ],
  "limits":   [ { "key": "maxVariants", "name": "Variants per sweep", "default": 64 } ]
}
```

and a generator that emits from it:

```csharp
public static class WitSweepLicense
{
    public static class Features { public const string FormatNas = "format.nas"; }
    public static class Limits   { public const string MaxVariants = "maxVariants"; }

    public static LicensingOptions Declares(this LicensingOptions options) => …;
}

// call sites become compile-checked
if (!licensing.HasFeature(WitSweepLicense.Features.FormatNas)) …
```

What that buys:

- **The stringly-typed call site becomes a compile error.** The one hazard with
  no current mitigation.
- **`Declares(...)` can no longer drift from the keys the product checks**, because
  both come from the same file.
- **The same file is the descriptor the registry imports** — `WitLicense/DESIGN.md`
  §6's V2 plan, which "removes the hazard at the root", needs a file to import
  and this is it. The manual-catalogue hazard collapses to "import the file the
  product already publishes".

Cost: a small incremental generator, shipped in `OutWit.Common.Licensing.MVVM`
or a sibling analyzer package, plus one JSON file per product. Cheaper than the
first support ticket it prevents, and it makes the V2 descriptor import a
data-plumbing task rather than a design task.

---

## 12. Staged plan

| Stage | Content | Done when |
|---|---|---|
| **V-1** | **The alignment wave** (§12.3) — central package management, `global.json`, one JWT stack, one MudBlazor, services onto `OutWit.Common` 1.4.x. **`AspectInjector` stays at 2.8.2** | Every repo builds green against one centrally pinned set; no package appears at two versions in the family except where a TFM condition explains it |
| **V0** | Library gaps 1–8 (§5) + `LicenseMode` + `LicenseSnapshot` → `OutWit.Common.Licensing` **1.1.0** | Mode, `StateChanged`, periodic re-evaluation, composite/env store and the snapshot exist and are tested — **done** |
| **V1** | `OutWit.Common.Licensing.MVVM` **1.0.0** (§7) — gateway, local gateway, panel VM, the ~~two~~ **three** seams (§7.3) | The **harness** binds to it (§7.5) and nothing about the harness's behaviour changed — **done**, and it cost the design one correction |
| **V2** | Extend the Avalonia harness (§6.1) | Mode is visible; a real key ring is loaded; a real licence issued by `license.omnibuscloud.com` and delivered **by email** installs and validates; a staged renewal switches over at `exp` |
| **V3** | Issuing-side blockers §10.1–§10.5 | Four products in the catalogue; four key rings exported; short-term presets live; the `appVer` default and its `Unlimited` rule enforced by the form; the binding kind and threshold are choosable |
| **V4** | The containerised mock (§6.2), the key-ring generator (§11.7.3) and the vocabulary generator (§11.8.3) | `installId` from `.env` survives `--force-recreate` and the fallback file form works when it is unset; two containers produce two distinct fingerprints and neither accepts the other's licence; URL normalisation survives a trailing slash; env var and file drop both work; a licence applies with no restart; feature keys are compile-checked |
| **V5** | **WitSweep** (§8) — Settings screen + Licence section + gate | Demo on first launch, panel reachable in every mode, Run gated with a distinct message, licence installs from a paste, level 0 verified untouched |
| **V5′** | **The Inventor add-in** — settings dialog + ribbon gate. Scheduled by when the add-in itself exists, not by this plan | The add-in loads and its ribbon appears in every mode; commands are visibly disabled with a reason rather than silently inert; the settings dialog shows the fingerprint, exports a request and accepts a licence; the panel ViewModel is bound from **WPF** with no change to the package |
| **V6** | `OutWit.Shared.Licensing.*` (§7.4) — contracts, host, Blazor | The containerised mock (V4) is re-pointed at the real admin-guarded channel and the shared host wiring. **The Blazor half is not proven here** — see §12.1 |
| **V7** | **WitCloud** (§9.1–§9.6) — the five lines + gates | Demo at first start, page + `/health`, node and job gates, cap-as-throttle, grace and banner, `Reconnect` deliberately ungated |
| **V8** | **WitIdentity** (§9.7) — the same five lines + its two gates | Authentication verified untouched in `Restricted`; new accounts and new clients refused with a licensing reason; the two services carry **two independent licences** and each refuses the other's |
| **V9** | Stage 4 onward — `checkIn` and the client-side periodic confirmation | Out of scope here |

V-1 comes first and alone (§12.3). V0–V4 are then sequential; V5 (client
family) and V6–V8 (service family) are independent of each other once V4 lands,
and V2/V3 interleave — V2's "real
licence" step *is* V3's first exercise of the issuing path, and running them
together is what finally closes the full production cycle end to end.

Three ordering constraints carry real weight:

- **V1 before V5.** The panel abstraction is validated by the harness while it
  is still free to change; by the time WitSweep adopts it, its shape is settled.
  Building WitSweep's panel first and extracting the shared piece afterwards
  would invert that, and the extraction would be shaped by whichever product
  happened to go first.

  **V5 and V5′ together are what actually test §7.1**, and they test it from
  opposite ends. WitSweep is nearly finished, so licensing it is a *retrofit* —
  and something can always be fitted into a product that already exists. The
  add-in has not been written, so licensing it is *greenfield*, and that is the
  honest measurement: what it costs to license a product from the start is the
  number §7.1 claims. The pair also settles the portability claim in §7.3, since
  one binds the same ViewModel from Avalonia and the other from WPF.
- **V6 before V7.** WitCloud is the *first consumer* of
  `OutWit.Shared.Licensing.*`, not its author. Writing a bespoke channel and page
  in WitCloud "and generalising later" is how four services end up with four
  channels — the exact outcome this design exists to avoid. The containerised
  mock from V4 is what lets V6 be built and proven with no service depending on
  it yet.
- **V7 and V8 ship together, and V8 is the acceptance test for the whole
  service design.** WitIdentity is the second consumer of
  `OutWit.Shared.Licensing.*`, so it is where §7.1 is either true or false. It
  should cost the five lines of §9.3 plus its two gates, and the pair should end
  up with **two independent licences, each refused by the other instance**
  (§7.8). If either fails, the seam is wrong and V6 gets corrected before a third
  service copies it — which is far cheaper now than after WitForms has been
  cloned from it.

### 12.1 Audit — where this plan is most likely to be wrong

Stepping back from the detail. Ranked by expected cost, not by likelihood.

**1. The mock cannot prove the Blazor half of V6.** The containerised mock
(§6.2) is a minimal API; it has no Blazor host, so `LicensePanel`,
`LicenseBanner` and `LicensePageViewModel` get their first real consumer in
WitCloud at V7. The stated ordering — *V6 before V7 so WitCloud is a consumer,
not an author* — therefore holds for the **channel and host** and only
partially for the **UI**. Expect V7 to feed corrections back into V6. That is
acceptable and it should be planned for rather than discovered; what is not
acceptable is letting the UI half be written inside WitCloud and extracted
afterwards, because that is the outcome §7.1 exists to prevent.

**2. Version drift is ecosystem-wide, not a MudBlazor problem.** The audit
originally recorded "two MudBlazor versions". Measuring found **four**, plus an
Avalonia 11/12 *major* split across the desktop family, **three versions of the
JWT validation stack**, EF Core and Npgsql drifting inside one TFM band, two
Roslyn versions under the source generators, and no central pinning or
`global.json` anywhere. §12.3 makes this stage **V-1**. It is the one audit
finding that became scheduled work rather than a caveat — and the one where
looking at the data changed the conclusion rather than confirming it.

**3. Eazfuscator on WitSweep is unproven.** The precedent is
`OutWit.Cloud.Client` (`client-release.yml`), a different build shape. String
encryption over a generated `const` ring (§11.7.3) and anti-tamper over an
Avalonia single-file publish are both plausible and neither has been run. If it
does not work, §11.7's conclusions stand but their value drops to zero for the
one product that is obfuscated at all.

**4. Three factors at n-of-n is the strictest thing in this document.** Every
argument for it is sound (§7.8.3) and it is still the decision most likely to
generate support volume — a domain migration, a staging clone, an identity-server
move all become Transfers. The mitigation is already designed (transfers are a
V1 capability, the diagnostic is "frequent transfers mean wrong factors"), and
§7.8.3 establishes that factors can be dropped later without invalidating
anything. Watch the transfer rate on the first three customers and treat it as
data, not as noise.

**5. Nothing in this plan has a customer in it.** Every acceptance criterion in
§12 is met on our own machines. The first real proof is the first on-prem
install, and the failure modes that matter most — a read-only volume, an
unreachable database at startup, a URL with a trailing slash, a clock two years
out — are exactly the ones a developer's laptop never produces. The container
mock is the substitute, and it is a substitute.

**What the audit did *not* find**, worth recording so it is not re-litigated:
the identity/entitlement separation (§2) survives every scenario raised across
this design; the format needs no change for anything here; and no decision in
this document is a one-way door except the `Unlimited` + unbounded-`appVer`
combination, which §10.4 refuses at the form.

### 12.2 Effort, and what to cut

The opening of §1 estimated "about six weeks" for stage 3. That is no longer
honest: the scope has grown by WitIdentity, three shared packages, two source
generators, a Settings screen, a container mock and the alignment wave that
turned out to be a prerequisite rather than housekeeping. A realistic shape:

| | Estimate |
|---|---|
| V-1 — the alignment wave (§12.3) | ~1.5–2 weeks |
| V0–V4 — library, kit, harness, issuing side, mock, generators | ~4 weeks |
| V5 — WitSweep, of which the Settings shell is ~1 day | ~1.5 weeks |
| V6–V8 — shared service packages, WitCloud, WitIdentity | ~3 weeks |
| **Total** | **~10–11 weeks** |

If that has to compress, cut in this order — each line is a real loss, listed
cheapest-first:

1. **The vocabulary generator** (§11.8.3). Valuable, not blocking. Costs the
   compile-checked feature keys; the unknown-key report still catches the
   catalogue side.
2. **Settings sections other than Licence** in WitSweep. Ship the Licence
   section into a shell whose other tabs arrive later.
3. **`issuer` as a third factor.** Ship with two; §7.8.3 makes adding it later
   free for already-issued licences.
4. **WitIdentity (V8) slips behind WitCloud (V7).** The capability exists; only
   the second-consumer proof is deferred — and with it, the evidence that §7.1 is
   true.

**Do not cut the container mock (V4).** It is the only thing standing between
this design and discovering §9.8, §4.3 and the URL-normalisation trap at a
customer site instead of on a laptop. Two of those three were found by *thinking*
about a container; the third kind is found only by running one.


### 12.3 V-1: the alignment wave — a prerequisite, not housekeeping

A survey of `Common` and `Shared` (78 packable projects) says the ecosystem has
drifted enough that licensing would inherit the drift rather than avoid it. The
findings, measured rather than assumed:

**Third-party versions, measured across all eight repositories.** Grouped by
family, because they do not all deserve the same urgency:

| Family | What is in use | Why it matters |
|---|---|---|
| **Security** — `System.IdentityModel.Tokens.Jwt`, `Microsoft.IdentityModel.Protocols.OpenIdConnect` | **8.14.0, 8.16.0, 8.18.0** | **The one to fix first.** This is the token-validation stack. Three versions means patching one advisory is three separate changes, and the odds of missing one are exactly as high as they look |
| **UI — Blazor** | MudBlazor **8.15.0, 9.1.0, 9.4.0, 9.5.0**; `MudBlazor.FontIcons` 1.3.0/1.4.0; `Components.WebAssembly` 10.0.2/10.0.5/10.0.8 | Four MudBlazor across five repos; WitIdentity carries two by itself. Blocks V6 |
| **UI — Avalonia** | **11.3.11, 11.3.12, 12.0.3, 12.1.1**; `Material.Avalonia` 3.13.4/3.16.1/3.17.0; `DialogHost.Avalonia` 0.10.4/0.12.2 | **A major split, not a patch spread.** Avalonia 11 → 12 is a migration. WitSweep is on 12.0.3, the harness on 12.1.1, other desktop projects still on 11.3.x. Directly shapes V5's panel view |
| **Data** | EF Core `Relational` 8.0.24 / 9.0.6 / 9.0.13 / 10.0.1 / 10.0.2 / 10.0.8; `Npgsql.EntityFrameworkCore.PostgreSQL` 10.0.0 / 10.0.1; `EntityFrameworkCore.Sqlite` 9.0.6 / 10.0.2 | Part of the EF spread is legitimate TFM-conditioning; **10.0.1 vs 10.0.2 vs 10.0.8 inside one band is not.** Npgsql matters more than it did — WitLicense now runs it in production |
| **Serialization** | `MessagePack` 3.1.4/3.1.8; `System.Text.Json` 8.0.5/10.0.0/10.0.2; `Serilog` 4.1.0/4.3.0/4.3.1 | |
| **Build / generators** | `Microsoft.CodeAnalysis.CSharp` **4.14.0 and 5.0.0**; **no `global.json` in any repo** | The source-generator surface, and §11.8 adds two more generators. With the SDK unpinned, a machine with a newer SDK compiles against a different Roslyn than CI does — reproducibility is currently a coincidence |
| **Test stack** | `Microsoft.NET.Test.Sdk` 17.10.0–18.0.1; NUnit 4.2.2–4.5.1; adapter 4.6.0–6.0.1; analyzers 4.4.0–4.11.2; coverlet 6.0.2/6.0.4 | Lowest risk in the list — do it first to prove the plumbing |
| **`Microsoft.Extensions.*`** | 8.0.x / 9.0.x / 10.0.x | **Mostly legitimate** — TFM-conditional. The real drift is *within* a band: `DependencyInjection.Abstractions` at 9.0.7, 9.0.8 **and** 9.0.12 |

##### One package is explicitly excluded

> **`AspectInjector` stays at 2.8.2. It is not part of this or any future sweep.**
> Upgrading it has been tried and it breaks. It is referenced directly by exactly
> two packages — `OutWit.Common.Aspects` and `OutWit.Common.DependencyInjection`
> — and reaches everything else transitively, including the `[Inject]` /
> `[InjectableHost]` machinery in every WitCloud channel. IL weaving fails in the
> worst possible shape: the build is green and the behaviour is wrong at runtime.
>
> When `Directory.Packages.props` lands, pin it there **with this comment beside
> it**, so the next person running an "update everything" sweep is stopped by the
> file rather than by a regression.

**OutWit's own versions:** services run `OutWit.Common` **1.3.x** while `Common`
ships **1.4.0** — and `OutWit.Common.Licensing` already depends on `1.4.*`. So
**adopting licensing forces a Common bump on every service that adopts it.**
Better done deliberately, once, than discovered at V7 under a deadline.

**Target frameworks are inconsistent in three different ways:**

- **End-of-life targets still shipped**: `net5.0` in `OutWit.Common.Reflection`
  and `OutWit.Common.Settings.NuGet`; `net6.0` and `net7.0` across most of
  `Common`. All three have been out of support for between one and three years.
- **`net10.0` missing** from `OutWit.Common.MEF`, `OutWit.Common.Settings.NuGet`
  and `OutWit.Common.Prism.NuGet`, while everything around them has it.
- **No central package management anywhere** — no `Directory.Packages.props` in
  `Common`, `Shared`, `WitCloud` or `WitIdentity`. Every version is pinned in its
  own `.csproj`, which is precisely how four MudBlazor versions happen.

#### What V-1 does

1. **Central package management** — `Directory.Packages.props` per repo,
   third-party versions pinned there exactly, TFM-conditional versions expressed
   as conditional `<PackageVersion>` groups. This converts drift from a recurring
   chore into a single reviewable file, and it is the only item here that
   prevents the problem returning.
2. **`global.json` per repo**, pinning the SDK with `rollForward: latestFeature`.
   Currently absent everywhere, which means the Roslyn a developer compiles
   against is whatever their machine has. That is tolerable until source
   generators matter, and §11.8 makes them matter.
3. **Unify the test stack first** — `Microsoft.NET.Test.Sdk`, NUnit, the adapter,
   the analyzers, coverlet. Lowest risk in the list, and it proves the CPM
   plumbing before anything load-bearing depends on it.
4. **Unify the security stack** — one `System.IdentityModel.Tokens.Jwt` and one
   `Microsoft.IdentityModel.*` across the family. Highest value per hour of
   anything in V-1, and the only item with a security argument rather than a
   maintenance one.
5. **Unify MudBlazor across `Shared` and all four services**, at one version.
   Non-negotiable before V6, because `OutWit.Shared.Licensing.Blazor` otherwise
   adds a *fifth* pin to a four-version spread and makes the problem permanent.
6. **Align EF Core and Npgsql within each TFM band**, keeping the legitimate
   8.0.x / 9.0.x / 10.0.x conditioning and removing the accidental
   10.0.1 / 10.0.2 / 10.0.8 spread. Npgsql is now a production dependency of
   WitLicense, so this is no longer only about tidiness.
7. **Add the missing `net10.0`** to the three packages lacking it. Pure addition,
   safe as a minor.
8. **Bump the services to `OutWit.Common` 1.4.x**, which licensing requires
   anyway.
9. **Settle pinning discipline**: third-party exact in the central file;
   OutWit-internal floating within a band (`1.4.*`), which is already the
   practice in some places and exact-pinned in others.

#### What V-1 deliberately does not do

**The end-of-life TFM cull is a separate, later wave, and it is not minor.**
Removing `net6.0`/`net7.0`/`net5.0` is a breaking change for any consumer on
them, so it is a **major** bump — the request was for minor updates, and these
two things cannot be the same release. Doing it in dribs across ~40 packages is
worse than doing it once, and it pays for itself when it lands, because dropping
those targets deletes most of the TFM-conditional `Microsoft.Extensions.*`
blocks that produce half the apparent version spread.

**The Avalonia 11 → 12 convergence is also out of scope here**, for the same
reason in a different form: it is a framework migration, not a version bump.
V-1 pins whatever each desktop project is already on and stops the *patch* drift
(11.3.11/11.3.12/11.3.15/11.3.17, 12.0.3/12.1.1); choosing when the 11.x
projects move to 12 is a separate decision. What V-1 must settle is narrower and
does block V5: **WitSweep and the licensing harness on one Avalonia 12
version**, so the licence panel is written once against one API.

**And `AspectInjector` is excluded permanently**, per the box above — not
deferred, excluded.

#### What the first repository actually cost

`Common` was done first, and the record is worth keeping because it calibrates
the rest.

**Landed:** `global.json` (SDK 10.0.300, `rollForward: latestFeature`),
`Directory.Packages.props` with 43 flat and 10 TFM-conditional `PackageVersion`
entries, and 70 project files stripped of inline versions. Baseline was
0 errors; after the wave, 0 errors and **51 test assemblies green across
net6–net10 with no failures**. Elapsed: well under a day, not the estimated
week — because the work is mechanical once the data is extracted rather than
guessed at.

**Four things it taught, each of which will repeat in the other repositories:**

1. **Floating versions need `CentralPackageFloatingVersionsEnabled`.** NU1011
   otherwise rejects the whole build. Our own `OutWit.*` packages float by
   design, so this is not optional — and it is the switch that keeps
   third-party pins exact while internal ones stay loose.
2. **The test stack cannot simply go to latest.** `Microsoft.NET.Test.Sdk` from
   17.11 onward and `NUnit3TestAdapter` 6.x both refuse `net6.0`/`net7.0` —
   hard error and framework fallback respectively. Both are held at the last
   version that accepts them, in a conditional group with a comment saying it
   disappears with the EOL wave. **The tooling has already dropped these
   targets; the cull is overdue rather than optional.**
3. **The advisories were already there.** Central management surfaced
   `MessagePack` 3.1.4 (high), `System.Security.Cryptography.Xml` 9.0.0 (high)
   and `AngleSharp` 1.4.0 (moderate) — but a stashed baseline build reports the
   identical counts, so alignment did not introduce them, it made them
   countable. **Every affected project is a test or a sample; no published
   package is exposed.** Listed as a follow-up, not folded into this wave.
4. **`nuget.config` cannot carry the source mapping.** NU1507 asks for it as
   soon as CPM meets two feeds, but the file is `.gitignore`d because it holds a
   GitHub credential, so a committed mapping is impossible until credentials
   move to a user-level config. Suppressed with that reasoning written next to
   it, because 756 copies of one warning hide the ones that matter.

#### Cost and placement

Roughly **1.5–2 weeks**: about a week for `Common` and `Shared` plus the central
files, then a day per service for the bump and a green test run.

It goes **before V0**, on its own, gated by the test suites — not interleaved
with feature work. Alignment front-loads risk by design: a bad wave breaks
everything at once, which is exactly why it must not be diagnosed while
something else is also new.

If it cannot be afforded in full, the minimum that unblocks the licensing work
is **items 5 and 8** — MudBlazor and the `OutWit.Common` bump — plus the
WitSweep/harness Avalonia alignment noted above. Item 4 (the JWT stack) should
still be done even if everything else slips, because its argument is not
maintenance. Items 1, 2, 3, 6, 7 and 9 are what stop the drift recurring;
skipping them means doing this again, with more repositories in it.
---

## 13. Open questions

1. **WitSweep renewal grace — 0 or a few days?** §3.1 proposes 0 on the argument
   that a person is present and level 0 keeps their data reachable. A small
   non-zero grace (3 days) would cover the "expired while I was on the plane"
   case. Commercial call, not a technical one.
2. **Threshold-1 licences for Wi-Fi-only laptops** (§4.2) — is a single
   `machine-id` factor an acceptable binding to sell, or does that class of
   machine get a shorter term instead?
3. **Does the public hosted instance carry a licence at all?** §9.5 argues yes,
   `kind: none` and unlimited, for the one-code-path reason. Cheap either way,
   but decide once rather than per deployment.
4. **Trial self-service** — still open from `WitLicense/DESIGN.md` §11.2. Not
   blocking: the demo already works with no contact, and the question is whether
   to keep it that way.
5. **WitIdentity's own metering.** §9.7 proposes `maxUsers` and `maxClients` as
   the caps and new-account / new-client creation as the gated verbs. That is a
   defensible default and it is safe, but it is a *pricing* claim as much as a
   technical one — whether an on-prem identity server is sold by seats at all
   should be settled before its catalogue entry is typed (§10.1).
6. **Whether WitForms and WitAnalytics are ever licensed.** The capability now
   costs five lines (§7.4), so this is purely commercial and needs no decision
   until one of them is sold on-prem.

**Decided since the first draft:**

- **WitSweep gets a full Settings screen** with a Licence section (§8.3,
  option A), not a licence-only screen.
- **WitCloud and WitIdentity are both licensed from the first stage**; every
  other service keeps licensing as an option (§7.8).
- **The licensed unit for a service is an instance, not a deployment.** One
  licence, one fingerprint, one paste per instance — a stack of two services is
  two documents, and a second WitCloud is a third. An earlier draft proposed a
  shared deployment identity to make a stack present one fingerprint; that is
  withdrawn (§7.8), and withdrawing it also removes a pre-deployment schema
  change from §10.
- **The service binding is `installId` + `publicBaseUrl` + `issuer` at n-of-n**,
  with `installId` generated at deploy into **`.env`** (§7.8.3) — not the
  database, and not a volume file except as a fallback. WitIdentity drops
  `issuer`, which for it is its own URL. Adding or dropping a factor later
  invalidates nothing already issued.
- **`appVer` policy**: the range mechanism already in the format does the work —
  the form prefills `>={current} <{next major}`, `Unlimited` plus an unbounded
  range is refused (§10.4), `WrongVersion` is never treated as expiry and never
  granted grace, and the upgrade is guarded before it happens rather than
  discovered afterwards (§3.4).

None of 1–6 blocks V0–V4.
