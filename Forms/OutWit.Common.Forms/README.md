# OutWit.Common.Forms

A form described as data: groups, tabs, fields, options and the conditions that decide what is live.
Serialisable, transferable, and enough for anybody to draw the form and keep it consistent while
somebody types.

Nothing here knows what a field means. That is the point: a plugin can contribute a screen without
shipping controls, and a second front end — a browser, a remote console, a different toolkit —
becomes a renderer rather than a rewrite.

## Contents

### Model (`OutWit.Common.Forms.Model`)

- `FormSchema` — the form: a key, a title key, a version, its groups, and the shape they flow into
  (`Columns`, `ColumnWeights`, `Flow`)
- `FormGroup` — a heading with things under it. A `Section`; a strip of `Tabs` and the `Tab`s in
  it; or a `List` and the `Entry`s in it — each named, summarised by a `SummaryKey`, and marked on
  by `ActiveWhen` or by its switch. Optionally a `SwitchKey` naming the boolean drawn as the group's
  own on/off; `Columns`, `ColumnWeights` and `Span` for shape
- `FormField` — a key, a kind, bounds, a unit or a quantity, options, a default, a span; whether it
  is read-only, optional, or a mirror of a field declared elsewhere; and when it is enabled or
  visible
- `FormFieldKind` — `Text`, `Label`, `Number`, `Duration`, `Boolean`, `Choice`, `MultiChoice`,
  `Range`, `Date`, `Quantity`, `Tokens`. Eleven, and a kind exists only where it changes what a
  renderer has to do
- `FormOption` — an answer a field offers, with its own visibility
- `FormCondition` — a comparison against another field's value, or a tree of them
- `FormValues` / `FormValue` — what is currently in the form, and where each value came from
  (`Set`, `Default`, `Read`, `Estimated`, `NotRead`, `NotAvailable`)
- `FormValidation` / `FormIssue` — what the authority says about the whole of it
- `FormPlacement<T>` — where a field or a group ended up: row, column, span. Worked out by whoever
  draws, never serialised

### Contract (`OutWit.Common.Forms.Interfaces`)

- `IFormValidator` — whoever declared the form, answering what the values mean together

### Evaluation (`OutWit.Common.Forms.Utils`)

- `FormConditionExtensions` — `IsMet`, `IsEnabled`, `IsVisible`, `IsActive`: answered locally, on
  every change, without asking anybody
- `FormSchemaExtensions` — walking the tree, plus `DuplicateKeys()` and `UnknownKeys()` for the
  structural mistakes that are otherwise invisible: two fields under one key, a condition on a
  field nobody declared, a group switched by a field it does not hold
- `FormLayoutExtensions` — `Placements()`, `Stacked()`, `Weights()`: the arithmetic that turns
  columns and spans into rows, kept here so that two renderers affording the same number of columns
  lay the same form out the same way

## The line it draws

**Structure** — what exists, and what is live while what else is set — is in the schema and is
evaluated where the form is drawn. A round trip to decide whether a control is greyed makes a form
feel broken.

**Meaning** — which combinations are acceptable, which limit depends on which other value — is the
authority's, asked through `IFormValidator` after a change. Copying that into a renderer is how two
implementations of one rule come to disagree.

## Shape, not measurement

`Columns`, `Span`, `ColumnWeights` and `Flow` are the only things here about arrangement, and they
say shape: that these answers are short enough to sit beside each other, which of them wants more
room, which way the sections fill the page. Not how wide anything is — the form does not know
whether it is being drawn in a popup, a page or a phone. A renderer with no room uses fewer columns,
and one column is always a correct answer.

## Serialisation

MemoryPack, natively: `[MemoryPackable]` with explicit `[MemoryPackOrder]`, so a schema crosses a
WitRPC boundary with nothing configured at either end.

No other format's attributes, deliberately — a model carrying them has made a private arrangement
with one serialiser. For a product standardised on MessagePack there is
`OutWit.Common.Forms.MessagePack`: one call, scoped to this model alone.

## Example

```csharp
var schema = new FormSchema("Recorder.Settings", "Forms.Title")
{
    Groups =
    {
        new FormGroup("Recording", "Forms.Recording")
        {
            Fields =
            {
                new FormField("RecordTime", "Forms.RecordTime", FormFieldKind.Choice)
                {
                    DefaultValue = "Time24",
                    Options =
                    {
                        new FormOption("Time24", "Forms.Time24"),
                        new FormOption("Time48", "Forms.Time48")
                    }
                },
                new FormField("Diary", "Forms.Diary", FormFieldKind.Boolean) { DefaultValue = "false" },
                new FormField("Diary.Kind", "Forms.Diary.Kind", FormFieldKind.Choice)
                {
                    EnabledWhen = FormCondition.On("Diary", FormOperator.IsTrue)
                }
            }
        }
    }
};

var values = FormValues.Of(schema);          // complete, at the defaults

values.Set("Diary", "true");

schema.Field("Diary.Kind")!.IsEnabled(values);   // true, decided locally
```

## License

MIT
