# OutWit.Common.Licensing.Generator

Two files per product in. Compile-checked keys and a tamper-resistant key ring out.

```bash
dotnet add package OutWit.Common.Licensing.Generator
```

| File | Becomes | Removes |
|---|---|---|
| `witsweep.product.json` | `WitSweepLicense` — feature and limit keys as consts | A mistyped feature key that silently disables a paid capability |
| `witsweep.keyring.json` | `WitSweepKeyRing` — the trusted public keys as a `const string` | A ring an attacker can swap for their own without touching an instruction |

Both are picked up by convention. A product that has to remember to register its
own files is one that will one day forget, and the symptom of forgetting is
silence rather than an error.

---

## Part one — the vocabulary

### The hazard it removes

A feature key lives in three places that must agree, and today only one of them
is checked by anything:

| Where | What happens when it is wrong |
|---|---|
| The registry catalogue, typed by an operator | The customer pays for `SSO`, the product checks `sso`, **nothing fails** and the feature is simply off |
| `Declares(v => v.Feature("sso", …))` | Affects only the unknown-key report |
| `HasFeature("sso")` at the call site | **A silent, permanent false.** No compiler error, no warning, and nothing in the unknown-key report — that report sees what the *licence* granted, never what the *code* asked for |

The third row had no mitigation at all, and it is the easiest mistake to make:
`HasFeature("ssoo")` compiles, runs, and quietly disables something a customer
paid for.

### The file

`witsweep.product.json`, committed in the product repo.

```jsonc
{
  "product": "WitSweep",
  "features": [
    { "key": "format.nas", "name": "Nastran decks" }
  ],
  "limits": [
    { "key": "maxVariants", "name": "Variants per sweep", "default": 64 }
  ]
}
```

Comments are allowed. The file is meant to be read and edited by people, and a
descriptor nobody may annotate is one nobody explains.

### What comes out

```csharp
public static class WitSweepLicense
{
    public const string Product = "WitSweep";

    public static class Features { public const string FormatNas = "format.nas"; }
    public static class Limits   { public const string MaxVariants = "maxVariants"; }

    public static void Declare(LicenseVocabulary vocabulary) { … }
}
```

```csharp
options.Declares(WitSweepLicense.Declare);

if (!licensing.HasFeature(WitSweepLicense.Features.FormatNas))
    return Refuse("This licence does not cover Nastran decks.");
```

### What it buys

- **The stringly-typed call site becomes a compile error.** The one hazard with
  no other mitigation.
- **`Declares(...)` cannot drift from the keys the product checks**, because both
  come from the same file.
- **The same file is the descriptor the registry imports.** The hand-typed
  catalogue hazard collapses to "import what the product already publishes".

---

## Part two — the key ring

### Why a constant and not a resource

There are two ways past an offline verifier, and they are not equally cheap:

| | **Removal** — patch the gate | **Substitution** — replace the trusted key |
|---|---|---|
| Requires | Reading and editing control flow | Finding and replacing a string |
| Result | A patched binary with a broken invariant | A binary that **genuinely validates** — every check passes, honestly |
| Reapplying to the next release | Re-find the method | Re-run the same byte replacement |
| Lets the attacker mint *arbitrary* licences | No | **Yes** — any customer, any term, unlimited |

Substitution is strictly the better attack, and it is the only one with a cheap
defence. An **embedded resource** is the worst place to keep the ring: a blob in
the assembly manifest, visible in any decompiler's resource view, findable with
`strings`, and — decisively — *not* a string literal in IL, so string encryption
does not cover it. A `const string` is the same data in the shape an obfuscator
can defend.

This is tamper-resistance, not secrecy. The ring holds public keys, it stays
readable with effort, and the generated class lists every trusted `kid` in its
doc comment: a customer's security team is entitled to see what the product
verifies against.

### The files

`witsweep.keyring.json` — exported by the issuing service, committed. The export
is what stops anyone copying PEM blocks by hand and eventually copying the wrong
one.

`witsweep.dev.keyring.json` — optional. Its keys are emitted under `#if DEBUG`,
so a development licence is worthless against a shipped build with no runtime
check enforcing it.

### What comes out

```csharp
public static class WitSweepKeyRing
{
    public const string Product = "WitSweep";

    private const string RING = "{\"keys\":[…],\"product\":\"WitSweep\"}";

    public static ILicenseKeyRing Create() { … }
}
```

```csharp
options.WithKeyRing(WitSweepKeyRing.Create());
```

The ring is re-emitted rather than copied. The file may carry comments and
trailing commas; the runtime reader accepts neither, and a verbatim copy would
compile happily and then parse to an empty ring at startup — a product that
trusts nothing and can only say "unknown key id". Members are ordered, so the
same file always yields the same constant and a diff means something.

### What it catches

Every way a ring can be wrong currently fails **closed and silently**, and
arrives at a customer site as "licence invalid" with nothing to say which side is
wrong:

| In the file | At runtime, today | At build time, now |
|---|---|---|
| A key with no `kid` | Dropped without a word | Error |
| The same `kid` twice | The second silently wins | Error |
| `"alg": "ES257"`, `"policy": "Development"` | The reader throws and returns an **empty ring** — every key lost to one word | Error |
| A key naming no products | Covers nothing | Warning |
| No key covering the ring's own product | Refuses every licence | Warning |

## Diagnostics

| Id | Severity | Meaning |
|---|---|---|
| `OWL001` | Error | The product descriptor could not be read, with the reason and the offset |
| `OWL002` | Error | The key ring could not be read, or would trust less than it appears to |
| `OWL003` | Warning | The ring parses, but part of it trusts nothing |
| `OWL004` | Error | Two rings claim the same product — file ordering must not decide it |

`OWL001` and `OWL002` are errors rather than warnings on purpose: falling back to
an empty vocabulary or an empty ring would reintroduce exactly the silent failure
this package exists to remove.

## Licence

Apache-2.0.
