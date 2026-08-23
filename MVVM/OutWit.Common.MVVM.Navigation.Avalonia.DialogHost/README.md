# OutWit.Common.MVVM.Navigation.Avalonia.DialogHost

Shows [OutWit.Common.MVVM.Navigation](../OutWit.Common.MVVM.Navigation/README.md) dialogs
through [DialogHost.Avalonia](https://github.com/AvaloniaUtils/DialogHost.Avalonia), for an
application already themed with Material.Avalonia that wants its dialogs to match the rest
of it.

It is a separate package on purpose: the navigation packages ship two hosts of their own —
a modal window and an overlay layer — and neither costs an external dependency. Take this one
only if you already have DialogHost.Avalonia in the application.

## Setup

```csharp
services.AddNavigation(nav => ...);
services.AddAvaloniaNavigation(o => o.UseDialogHost<DialogHostAvaloniaAdapter>());
```

```xml
<!-- somewhere near the root of the window -->
<dialogHost:DialogHost Identifier="Root" CloseOnClickAway="True">
  <!-- the application's content -->
</dialogHost:DialogHost>
```

The `Identifier` matches the host name a dialog is shown on — `Root` is the default, and
`DialogHosts.ROOT` is the constant for it. Nothing above changes: view models still implement
`IDialogAware<TResult>` and never learn which host showed them.

## What the adapter is for

`DialogHost.Avalonia` closes a dialog the moment the user clicks away, and the navigation
contract says every close the user starts has to go through the dialog's own `CanCloseAsync`
first — a screen with unsaved changes gets to say no. The adapter is the piece that tells the
two kinds of close apart: it vetoes the user's, asks, and closes for real only if the dialog
agrees. A close the dialog service asked for is not put to the vote.

Dialogs do not nest: `DialogHost.Avalonia` keeps one session per identifier, so
`SupportsNesting` is false and a second `ShowAsync` on a busy host comes back cancelled with a
warning in the log — the same behaviour as the built-in overlay host.

## License

Apache-2.0. Part of the [OutWit](https://github.com/dmitrat/Common) ecosystem.
