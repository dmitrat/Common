#### Install

```ps1
Install-Package OutWit.Common.Logging
```

or

```bash
> dotnet add package OutWit.Common.Logging
```

## Streamlining Logging with the `Log` Aspect

Maintaining logs is critical for debugging and supporting complex applications. Manually adding logging calls everywhere can be tedious and error-prone. The `OutWit.Common.Logging` package simplifies this process with three powerful aspects: `Log`, `NoLog`, and `Measure`. The source code is available [here](https://github.com/dmitrat/OutWit/tree/main/Sources/Common/OutWit.Common.Logging).

### Setting Up the Logger

Before using the logging aspects, initialize the logger. The current implementation is based on **Serilog**:

```csharp
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Is(LogEventLevel.Information)
    .Enrich.WithExceptionDetails()
    .WriteTo.File(@"D:\Log\Log.txt",
        rollingInterval: RollingInterval.Day,
        rollOnFileSizeLimit: true,
        fileSizeLimitBytes: 524288)
    .CreateLogger();
```
### The `Log` Aspect
The `Log` aspect can be applied to individual methods:
```csharp
public class Model
{
    [Log]
    public void DoSomething1()
    {
    }

    public void DoSomething2()
    {
    }
}
```
Or to an entire class:
```csharp
[Log]
public class Model
{
    public void DoSomething1()
    {
    }

    public void DoSomething2()
    {
    }
}
```
When applied to a class, it automatically applies to all methods within the class.

**What Does the Log Aspect Do?**
1. **Exception Handling**: Wraps method execution in a try-catch block and logs exceptions with their details.
2. **Method Execution Logging**: Logs method calls (excluding property getters/setters) if the logger’s MinimumLevel is Information or lower.
3. **Detailed Property Access Logging**: If the logger’s MinimumLevel is Verbose, it also logs property access.

This flexibility allows you to control log verbosity by adjusting the logger's configuration.

### The NoLog Aspect
If the `Log` aspect is applied to a class, but you want to exclude specific methods from logging, you can use the `NoLog` aspect:
```csharp
[Log]
public class Model
{
    public void DoSomething1()
    {
    }

    [NoLog]
    public void DoSomething2()
    {
    }
}
```
### The Measure Aspect
To measure the execution time of a method, apply the `Measure` aspect. This will log the duration in milliseconds:
```csharp
public class Model
{
    public void DoSomething1()
    {
    }

    [Measure]
    public void DoSomething2()
    {
    }
}
```
When `DoSomething2` is called, the log will include the method’s execution time.
### Key Benefits
- Automates logging for methods and properties.
- Simplifies exception handling by centralizing logging.
- Provides flexible control over log verbosity.
- Enables easy performance measurement.
  
For more details, check out [the article](https://ratner.io/2024/11/20/streamlining-net-development-with-practical-aspects/).
