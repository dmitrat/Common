# OutWit.Common.Email

Vendor-neutral email primitives for the OutWit ecosystem.

## Contents

- `IEmailTransport` — abstracts the byte-on-the-wire layer (SMTP, Resend, AWS SES, etc.).
- `EmailMessage` — a fully-rendered email (To/From/Subject/HtmlBody/TextBody/Headers/ReplyTo).
- `EmailSendResult` + `EmailFailureKind` — typed result so callers can decide retry / alert / mark-bad / fail-hard.
- `Templates/IEmailTemplateProvider` — resolves a template name to raw HTML.
- `Templates/IEmailTemplateRenderer` — substitutes placeholders against a model.
- `Templates/SimpleEmailTemplateRenderer` — regex-based default renderer. Supports `<!-- subject: ... -->`, `{{#Key}}...{{/Key}}` sections, and `{{Key}}` placeholders (HTML-escaped except for the special `ActionLink` key).
- `Templates/FileEmailTemplateProvider` — file-based default provider. Reads `<root>/<name>.html` with optional `IMemoryCache`-backed caching. Host-agnostic — takes a plain path, not `IWebHostEnvironment`.

## Usage

```csharp
services.AddSingleton<IEmailTemplateRenderer, SimpleEmailTemplateRenderer>();
services.AddSingleton<IEmailTemplateProvider>(sp =>
    new FileEmailTemplateProvider(
        Path.Combine(env.ContentRootPath, "Templates"),
        sp.GetService<IMemoryCache>(),
        sp.GetRequiredService<ILogger<FileEmailTemplateProvider>>()));

// A vendor-specific transport (Resend/SMTP/...) is registered separately.
services.AddSingleton<IEmailTransport, MyResendEmailTransport>();
```

## License

Apache 2.0 — see `LICENSE`.
