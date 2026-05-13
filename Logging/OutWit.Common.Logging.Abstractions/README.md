# OutWit.Common.Logging.Abstractions

Lightweight logging abstractions for the OutWit.Common.Logging ecosystem.

## Contents

- `ILogManager` — abstracts an `ILoggerFactory` host so applications can swap logger backends.
- `LogUtils` extensions — `ILogger.Measure(...)` to time an action and log its duration, `ILogger.Log(level, message, args)` to log with a runtime-chosen `LogLevel`.

## Why this package

Depend on `OutWit.Common.Logging.Abstractions` when you need only the interface surface (e.g. designing a library against `ILogManager`) without pulling Serilog and other implementation dependencies. The concrete logger (`SimpleLogger`, aspects) lives in `OutWit.Common.Logging`.

## License

Apache 2.0 — see `LICENSE`.
