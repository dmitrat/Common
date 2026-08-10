# OutWit.Common.Licensing.Generator

One file per product in. Compile-checked licensing keys out.

```bash
dotnet add package OutWit.Common.Licensing.Generator
```

## The hazard it removes

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

## The file

`witsweep.product.json`, committed in the product repo. Picked up by convention —
a product that has to remember to register its own vocabulary is one that will
one day forget, and the symptom of forgetting is an empty vocabulary rather than
an error.

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

## What comes out

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

## What it buys

- **The stringly-typed call site becomes a compile error.** The one hazard with
  no other mitigation.
- **`Declares(...)` cannot drift from the keys the product checks**, because both
  come from the same file.
- **The same file is the descriptor the registry imports.** The hand-typed
  catalogue hazard collapses to "import what the product already publishes".

## Diagnostics

`OWL001` — the descriptor could not be read, with the reason and the offset. It
is an **error**, not a warning: falling back to an empty vocabulary would
reintroduce exactly the silent failure this exists to remove.

## Licence

Apache-2.0.
