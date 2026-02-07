# OutWit.Common.Blazor.WitRPC

## Overview

OutWit.Common.Blazor.WitRPC is a Blazor WebAssembly channel factory for **WitRPC** communication over WebSocket.
It bridges the gap between the browser environment and the WitRPC server by handling:

- **RSA/AES encryption** entirely in the browser via the Web Crypto API
- **Automatic reconnection** with exponential backoff after disconnects
- **Retry policies** for transient failures (server errors, timeouts, I/O)
- **Authentication integration** — reconnects on sign-in, disconnects on sign-out
- **One-line DI registration** with optional configuration

All of this is exposed through a single `IChannelFactory` interface that lazily establishes a WebSocket connection and hands back typed service proxies.

#### Install

```ps1
Install-Package OutWit.Common.Blazor.WitRPC
```

or

```bash
dotnet add package OutWit.Common.Blazor.WitRPC
```

## Target Framework

`net10.0`

## Getting Started

### 1. Register Services

In `Program.cs`, call `AddWitRpcChannel()` on the service collection. The method registers `EncryptorClientWeb`, `ChannelTokenProvider`, and `IChannelFactory` in a single call:

```csharp
using OutWit.Common.Blazor.WitRPC;

builder.Services.AddWitRpcChannel(options =>
{
    options.ApiPath = "api";          // WebSocket endpoint path (default: "api")
    options.TimeoutSeconds = 15;      // Connection & request timeout (default: 10)
});
```

The registered service lifetimes are:

| Service | Lifetime | Why |
|---------|----------|-----|
| `ChannelFactoryOptions` | Singleton | Immutable configuration, shared across scopes |
| `EncryptorClientWeb` | Scoped | Holds per-session RSA/AES keys |
| `ChannelTokenProvider` | Scoped | Bridges the scoped Blazor `IAccessTokenProvider` |
| `IChannelFactory` ? `ChannelFactory` | Scoped | Manages one WebSocket connection per circuit |

### 2. Obtain a Service Proxy

Inject `IChannelFactory` into any component or service and call `GetServiceAsync<T>()`. The factory lazily connects on first use:

```csharp
@inject IChannelFactory ChannelFactory

@code {
    private IMyService? _service;

    protected override async Task OnInitializedAsync()
    {
        _service = await ChannelFactory.GetServiceAsync<IMyService>();
    }

    private async Task DoWork()
    {
        var result = await _service!.SomethingAsync();
    }
}
```

### 3. Force Reconnection

If you need to re-establish the connection (e.g. after a configuration change or server restart):

```csharp
await ChannelFactory.ReconnectAsync();
```

This tears down the existing `WitClient`, creates a new one with fresh encryption keys, and reconnects.

## Connection Lifecycle

`ChannelFactory` manages the WebSocket connection through a `SemaphoreSlim` gate to ensure thread safety.

```
                 GetServiceAsync<T>()
                        ?
                        ?
              ????????????????????
              ? Client == null?  ????? no ???? return proxy
              ????????????????????
                     ? yes
                     ?
              ????????????????
              ?  m_gate.Wait ?     (semaphore — one connect at a time)
              ????????????????
                     ?
              ????????????????????????
              ?  EncryptorClientWeb  ?     generates RSA-2048 via JS
              ?     .InitAsync()     ?     exports JWK ? byte[]
              ????????????????????????
                     ?
              ????????????????????????
              ?  WitClientBuilder    ?
              ?    .Build(options)   ?
              ?  ?? WebSocket(url)   ?     ws:// or wss:// from NavigationManager
              ?  ?? MemoryPack       ?     binary serialization
              ?  ?? Encryptor        ?     EncryptorClientWeb
              ?  ?? TokenProvider    ?     ChannelTokenProvider
              ?  ?? AutoReconnect    ?     unlimited, 1s ? 2min
              ?  ?? RetryPolicy      ?     3×, 500ms ? 10s
              ????????????????????????
                     ?
              ????????????????????????
              ?  client.ConnectAsync ?     with configured timeout
              ????????????????????????
                     ?
              ????????????????????
              ?  m_gate.Release  ?
              ????????????????????
```

The WebSocket URL is derived automatically from `NavigationManager.BaseUri`:

```
https://example.com/  ?  wss://example.com/api
http://localhost:5000/ ?  ws://localhost:5000/api
```

## Authentication Integration

`ChannelFactory` subscribes to `AuthenticationStateProvider.AuthenticationStateChanged` during construction:

| Event | Action |
|-------|--------|
| User signs in (`IsAuthenticated == true`) | Calls `ReconnectAsync()` — tears down old connection, creates new one with fresh token |
| User signs out (`IsAuthenticated == false`) | Disconnects and sets `Client = null` |
| Factory disposed | Unsubscribes from the event, disconnects, disposes semaphore |

`ChannelTokenProvider` obtains access tokens from Blazor's `IAccessTokenProvider` and makes them available to the WitRPC transport layer:

```csharp
// Internally:
var result = await TokenProvider.RequestAccessToken(new AccessTokenRequestOptions());
if (result.TryGetToken(out var token))
    return token.Value;
```

If the token request fails, an empty string is returned and an error is logged.

## Resilience

### Auto-Reconnect

The built-in auto-reconnect policy fires whenever the underlying WebSocket disconnects unexpectedly:

| Setting | Value |
|---------|-------|
| `MaxAttempts` | `0` (unlimited) |
| `InitialDelay` | 1 second |
| `MaxDelay` | 2 minutes |
| `BackoffMultiplier` | 2.0× |
| `ReconnectOnDisconnect` | `true` |

### Retry Policy

Individual RPC calls are retried on transient failures:

| Setting | Value |
|---------|-------|
| `MaxRetries` | 3 |
| `InitialDelay` | 500 ms |
| `MaxDelay` | 10 seconds |
| `BackoffType` | Exponential |
| Retried statuses | `InternalServerError` |
| Retried exceptions | `TimeoutException`, `IOException` |

## Encryption

The library implements end-to-end encryption between the browser and the WitRPC server using a two-phase handshake:

### Phase 1 — RSA Key Exchange

1. `EncryptorClientWeb.InitAsync()` calls `cryptoInterop.generateKeys(2048)` via JS interop
2. The browser generates an RSA-OAEP key pair using the Web Crypto API
3. Public and private keys are exported in JWK format
4. `DualNameJsonConverter` re-serializes the keys, converting Base64Url to standard Base64 and mapping JWK field names (`n`, `e`, `d`, `p`, `q`, `dp`, `dq`, `qi`) to .NET-compatible property names (`mod`, `exp`, `d`, `p`, `q`, `dp`, `dq`, `iq`)
5. The public key is sent to the server, which uses it to encrypt a symmetric AES key

### Phase 2 — AES Symmetric Encryption

1. The server encrypts an AES-CBC key + IV with the client's RSA public key
2. `EncryptorClientWeb.DecryptRsa()` decrypts it in the browser
3. `ResetAes()` stores the symmetric key and IV
4. All subsequent communication uses `Encrypt()` / `Decrypt()` with AES-CBC

### RSA Parameters Mapping

| JWK name | `JsonPropertyName` | C# property | Description |
|----------|---------------------|-------------|-------------|
| `n` | `"n"` | `mod` | RSA modulus |
| `e` | `"e"` | `exp` | Public exponent |
| `d` | `"d"` | `d` | Private exponent |
| `p` | `"P"` | `p` | First prime factor |
| `q` | `"q"` | `q` | Second prime factor |
| `dp` | `"dp"` | `dp` | d mod (p ? 1) |
| `dq` | `"dq"` | `dq` | d mod (q ? 1) |
| `qi` | `"qi"` | `iq` | CRT coefficient |

## JavaScript Interop

The library ships `wwwroot/js/cryptoInterop.js` which is automatically available as a static web asset. It exposes the following functions on `window.cryptoInterop`:

| Function | Parameters | Returns | Description |
|----------|------------|---------|-------------|
| `generateKeys` | `keySize` (int) | `void` | Generates RSA-OAEP key pair with SHA-256 |
| `getPublicKey` | — | `string` (JWK JSON) | Exports public key |
| `getPrivateKey` | — | `string` (JWK JSON) | Exports private key |
| `decryptRSA` | `encryptedBase64` (string) | `string` (Base64) | RSA-OAEP decryption |
| `encryptAes` | `base64Key`, `base64Iv`, `base64Data` | `string` (Base64) | AES-CBC encryption |
| `decryptAes` | `base64Key`, `base64Iv`, `base64EncryptedData` | `string` (Base64) | AES-CBC decryption |

> **Note**: The script uses `window.crypto.subtle` which requires a [secure context](https://developer.mozilla.org/en-US/docs/Web/Security/Secure_Contexts) (HTTPS or localhost).

## Configuration Reference

`ChannelFactoryOptions` provides the following settings:

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `ApiPath` | `string` | `"api"` | WebSocket endpoint path relative to the application base URL. Leading `/` is trimmed automatically. |
| `TimeoutSeconds` | `int` | `10` | Timeout in seconds for both the initial connection and individual RPC requests. |

## API Reference

### `IChannelFactory`

| Member | Returns | Description |
|--------|---------|-------------|
| `GetServiceAsync<T>()` | `Task<T>` | Returns a typed service proxy. Establishes connection on first call. Throws `InvalidOperationException` if connection cannot be established. |
| `ReconnectAsync()` | `Task` | Disconnects the current client and creates a new connection with fresh encryption keys. |
| `DisposeAsync()` | `ValueTask` | Unsubscribes from auth events, disconnects, and disposes internal resources. |

### `EncryptorClientWeb`

| Member | Returns | Description |
|--------|---------|-------------|
| `InitAsync()` | `Task<bool>` | Generates RSA keys via JS interop. Returns `false` if browser crypto is unavailable. |
| `GetPublicKey()` | `byte[]` | Serialized public key (JSON ? UTF-8 bytes). |
| `GetPrivateKey()` | `byte[]` | Serialized private key (JSON ? UTF-8 bytes). |
| `DecryptRsa(data)` | `Task<byte[]>` | RSA-OAEP decryption via JS interop. |
| `ResetAes(key, iv)` | `bool` | Stores AES-CBC symmetric key and IV for subsequent encrypt/decrypt calls. |
| `Encrypt(data)` | `Task<byte[]>` | AES-CBC encryption via JS interop. |
| `Decrypt(data)` | `Task<byte[]>` | AES-CBC decryption via JS interop. |

### `ChannelTokenProvider`

| Member | Returns | Description |
|--------|---------|-------------|
| `GetToken()` | `Task<string>` | Requests an access token from Blazor's `IAccessTokenProvider`. Returns empty string on failure. |

## Architecture

```
???????????????????????????????????????????????????????
?  Blazor WebAssembly Application                     ?
?                                                     ?
?    builder.Services.AddWitRpcChannel(options => ...) ?
?    var svc = await factory.GetServiceAsync<T>();     ?
???????????????????????????????????????????????????????
?  IChannelFactory ? ChannelFactory (sealed)          ?
?    ??? NavigationManager  (base URI ? ws:// / wss://)?
?    ??? AuthenticationStateProvider  (sign-in/out)   ?
?    ??? EncryptorClientWeb  (RSA/AES via JS interop) ?
?    ??? ChannelTokenProvider  (bearer access tokens) ?
?    ??? ChannelFactoryOptions  (ApiPath, Timeout)    ?
?    ??? ILogger<ChannelFactory>  (structured logs)   ?
???????????????????????????????????????????????????????
?  Encryption Layer                                   ?
?    EncryptorClientWeb ??? cryptoInterop.js           ?
?    RSAParametersWeb  (JWK ? .NET field mapping)     ?
?    DualNameJsonConverter  (Base64Url ? Base64)       ?
?    RsaUtils  (Base64Url padding helper)             ?
???????????????????????????????????????????????????????
?  Transport                                          ?
?    OutWit.Communication.Client.WebSocket (NuGet)    ?
?    WitClientBuilder  ?  WitClient                   ?
?    MemoryPack serialization                         ?
?    Auto-reconnect + Retry policies                  ?
???????????????????????????????????????????????????????
```

## Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| `Microsoft.AspNetCore.Components.WebAssembly` | 10.0.2 | Blazor WASM runtime, `NavigationManager`, `IJSRuntime` |
| `Microsoft.AspNetCore.Components.WebAssembly.Authentication` | 10.0.2 | `IAccessTokenProvider`, `AuthenticationStateProvider` |
| `OutWit.Communication.Client.WebSocket` | 2.3.1 | WitRPC client transport — `WitClientBuilder`, `WitClient`, resilience policies |

## Attribution

OutWit.Common.Blazor.WitRPC is part of the **OutWit** ecosystem.  
Copyright © 2020–2026 Dmitry Ratner.

## Trademarks

"OutWit" is a trademark of Dmitry Ratner.

## License

Licensed under [Apache-2.0](LICENSE).
