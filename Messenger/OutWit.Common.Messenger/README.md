# OutWit.Common.Messenger

Vendor-neutral messenger (instant-messaging) model for the OutWit ecosystem — the
counterpart of `OutWit.Common.Email` for chat/IM notifications.

It defines the byte-on-the-wire contract a host sends through, with no coupling to
any vendor SDK:

- **`IMessengerTransport`** — `SendAsync(MessengerMessage, CancellationToken)`.
- **`MessengerMessage`** — `Target` (chat id / channel / `@username`), `Text`,
  optional `Title`, `Format` (`Plain` / `Markdown` / `Html`), `SilentNotification`,
  `Metadata`.
- **`MessageSendResult`** — `Succeeded`, `FailureKind`, `ProviderMessageId`,
  `ErrorMessage` (with `Success` / `Failure` factories).
- **`MessengerFailureKind`** — `None` / `Transient` / `AuthFailure` /
  `InvalidRecipient` / `RateLimited` / `Permanent`.

Concrete transports (Telegram, Slack, …) ship as `OutWit.Shared.Messenger.Provider.*`
plugins loaded from a host's `@Messenger` folder and selected via
`Messenger:ProviderKey`.
