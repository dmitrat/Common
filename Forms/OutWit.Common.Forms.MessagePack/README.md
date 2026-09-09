# OutWit.Common.Forms.MessagePack

Carries `OutWit.Common.Forms` on a MessagePack wire.

The form model is described once, for MemoryPack, because that is what WitRPC speaks without being
asked — and it carries no other serialiser's attributes on purpose. A product standardised on
MessagePack adds this package and calls it once:

```csharp
FormsMessagePack.Register();
```

After that, `FormSchema`, `FormValues` and everything they are made of travel through that product's
own entry points — `ToMessagePackBytes`, `MessagePackClone` — with no attribute added to the model.

## Why a package and not one line

Registering `ContractlessStandardResolver` globally does the same job and loosens the whole
application: after it, *every* unattributed type becomes serialisable, so a model of your own that
has lost its `[MessagePackObject]` stops failing and quietly starts travelling in a different shape.

This registers a resolver scoped to the form model's assembly. Everything else is serialised exactly
as it was, and a foreign unattributed type is still refused.

## Why not hand-written formatters

A formatter per type is a second description of the model, kept by hand, which goes out of step the
first time a field is added. The resolver delegates to contractless for its own types and is four
lines long.

The cost is a slightly larger payload — member names rather than numbers — and a schema is sent once
per form.

## License

MIT
