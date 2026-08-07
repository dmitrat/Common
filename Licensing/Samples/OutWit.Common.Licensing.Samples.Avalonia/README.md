# OutWit.Common.Licensing — harness

A two-pane Avalonia app that closes the whole licensing loop in one window:
generate a key → produce a fingerprint → build a request → issue → install →
validate → expire → refuse.

**Not a tool.** It signs with throwaway keys it generates in-process and knows
nothing about any registry. The real issuing side is a separate, private
service; this exists to prove the library and to show a person what it does.

```bash
dotnet run --project Licensing/Samples/OutWit.Common.Licensing.Samples.Avalonia
```

Licences land in `sample-licenses/` next to the executable — a real folder,
inspectable while the app runs. **Wipe licences** returns the host to
factory-fresh.

## Why it exists

Three things a unit-test suite cannot cover, and each one has already earned its
keep:

**Cross-platform reality.** Tests exercise fakes. Whether `machine-id`,
`primary-mac` and `machine-name` are actually stable across a reboot, a docker
restart, a VM snapshot or an OS update is only answerable by running the thing
on Windows, Linux and macOS. This is the instrument for that.

**The loop has many joints.** keygen → fingerprint → request → issue → deliver →
install → validate → expire → renew. Each joint is where a format or path
assumption breaks, and better here than in a shipping installer.

**It is the conversation piece.** One window that shows a colleague — or a
customer's IT department asking "what exactly does your licensing do to my
machine" — the entire answer.

### Three defects it found on its first run

| Defect | Why no test caught it |
|---|---|
| **Startup deadlock.** `AddLicensing` blocks on an async pipeline; an `await` inside the library captured the UI synchronization context. Process alive, no window, nothing in the log. | A test runner has no synchronization context to capture. Now covered by `SynchronizationContextTests`, which installs one that never runs anything. |
| **Every OutWit product got the fingerprint prefix `WIT`.** Taking the first three letters of the product name is the obvious rule and is exactly wrong for a family whose names share a prefix. | Nothing asserted that two products differ. |
| **An expired *purchased* licence reported "the demo period has ended"**, hiding the expiry date, the customer name and the renewal actually needed. | The rule looked right in isolation; it only reads as wrong when you watch it happen. |

## The panes

### Product (left)

Consumes nothing but `ILicenseService` — the same surface a real product sees.

- **Status** with the sentence `Describe()` produces, plus a DEMO badge.
- **Fingerprint** — the code a customer reads to support.
- **What the licence grants** — limits and features read through the service, so
  demo caps, declared defaults and a real licence are visibly different things.
- **Unrecognised keys** — the typo report, in orange, when a licence grants
  something this build does not understand.
- **Run** — the gated action. Opening, viewing and requesting are never gated.
- **Install** — paste a token. An invalid one is refused and does **not**
  displace a working licence.
- **Installed on disk** — every document the store holds, because the store holds
  several so a renewal can be staged early.

### Issuer (right)

- **Signing key** — several throwaway keys with different algorithms, scopes and
  policies, including one deliberately left out of the product's ring.
- **Defect to introduce** — see below.
- **Term** — including *Expired last week*, *Starts in 20 days* and *Unlimited*.
- **Payload** — edition (a label; no product branches on it), features, limits,
  version range, and machine binding with an adjustable *n of m* threshold.

## Clock travel

The bar at the top moves the clock the product reads. It is the only honest way
to reach expiry, a staged renewal and clock tampering — waiting a year for each
is not a test. Moving **backwards** past what the store has already observed
triggers `ClockTampered`, which is the same lever a user reaching for a free
extension would pull.

## Defect modes

Each drives one refusal path. A status nothing can reach is a status nobody can
trust.

| Mode | Expected |
|---|---|
| `UnknownKey` | `UnknownKeyId` — signed by a key outside the product's ring |
| `WrongProduct` | `WrongProduct` |
| `OutOfScopeKey` | `ExceedsKeyPolicy` — a valid key, not scoped to this product |
| `MismatchedAlgorithm` | `SignatureInvalid` — header claims ES512, ring registers ES256 |
| `TamperedPayload` | `SignatureInvalid` — payload edited after signing |
| `BrokenToken` | `Malformed` |
| `ForeignMachine` | `BindingMismatch` |
| `TrialOverreach` | `ExceedsKeyPolicy` — a trial-only key asked for an unlimited term |
| `WrongVersion` | `WrongVersion` |

## House style

Built with `OutWit.Common.MVVM` — no code-behind beyond `InitializeComponent`,
`ViewModelBase<ApplicationViewModel>`, `[Notify]`, and gating recomputed in a
single `UpdateStatus()`. It doubles as the reference integration for the package.
