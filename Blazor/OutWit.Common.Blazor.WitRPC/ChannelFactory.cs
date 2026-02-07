using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Logging;
using OutWit.Common.Blazor.WitRPC.Encryption;
using OutWit.Communication.Client;
using OutWit.Communication.Client.WebSocket.Utils;
using OutWit.Communication.Model;
using OutWit.Communication.Resilience;

namespace OutWit.Common.Blazor.WitRPC
{
    /// <summary>
    /// Default implementation of IChannelFactory for WitRPC communication.
    /// Manages WebSocket connection lifecycle and authentication state.
    /// </summary>
    public sealed class ChannelFactory : IChannelFactory
    {
        #region Fields

        private readonly SemaphoreSlim m_gate = new(1, 1);
        
        #endregion

        #region Constructors

        public ChannelFactory(
            NavigationManager navigationManager, 
            AuthenticationStateProvider authenticationProvider,
            EncryptorClientWeb encryptor, 
            ChannelTokenProvider tokenProvider, 
            ChannelFactoryOptions options,
            ILogger<ChannelFactory> logger)
        {
            NavigationManager = navigationManager; 
            AuthenticationProvider = authenticationProvider;
            Encryptor = encryptor;
            TokenProvider = tokenProvider;
            Options = options;
            Logger = logger;
            
            InitDefaults();
            InitEvents();
        }

        #endregion

        #region Initialization

        private void InitDefaults()
        {
            IsDisposed = false;
        }

        private void InitEvents()
        {
            AuthenticationProvider.AuthenticationStateChanged += OnAuthenticationProviderChanged;
        }

        #endregion

        #region Functions

        private async Task EnsureConnectedAsync()
        {
            if (Client is not null) 
                return;

            await m_gate.WaitAsync();
            try
            {
                Client ??= await CreateClientAsync();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to connect client");
            }
            finally
            {
                m_gate.Release();
            }
        }

        private async Task<WitClient?> CreateClientAsync()
        {
            var baseUri = new Uri(NavigationManager.BaseUri);
            var wsScheme = baseUri.Scheme == "https" ? "wss" : "ws";
            var apiUrl = $"{wsScheme}://{baseUri.Authority}/{Options.ApiPath.TrimStart('/')}";

            await Encryptor.InitAsync();

            var client = WitClientBuilder.Build(options =>
            {
                options.WithWebSocket(apiUrl);
                options.WithMemoryPack();
                options.WithEncryptor(Encryptor);
                options.WithLogger(Logger);
                options.WithAccessTokenProvider(TokenProvider);
                options.WithTimeout(TimeSpan.FromSeconds(Options.TimeoutSeconds));

                options.WithAutoReconnect(reconnect =>
                {
                    reconnect.MaxAttempts = 0;
                    reconnect.InitialDelay = TimeSpan.FromSeconds(1);
                    reconnect.MaxDelay = TimeSpan.FromMinutes(2);
                    reconnect.BackoffMultiplier = 2.0;
                    reconnect.ReconnectOnDisconnect = true;
                });

                options.WithRetryPolicy(retry =>
                {
                    retry.MaxRetries = 3;
                    retry.InitialDelay = TimeSpan.FromMilliseconds(500);
                    retry.MaxDelay = TimeSpan.FromSeconds(10);
                    retry.BackoffMultiplier = 2.0;
                    retry.BackoffType = BackoffType.Exponential;

                    retry.RetryOnStatus(CommunicationStatus.InternalServerError);

                    retry.RetryOn<TimeoutException>();
                    retry.RetryOn<IOException>();
                });
            });

            try
            {
                var result = await client.ConnectAsync(
                    TimeSpan.FromSeconds(Options.TimeoutSeconds), 
                    CancellationToken.None);
                    
                if(!result)
                    throw new InvalidOperationException("Connection failed");
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Connection failed to {Url}", apiUrl);
                return null;
            }
            
            return client;
        }

        private async Task DisposeClientAsync()
        {
            if (Client != null)
                await Client.Disconnect();
            
            Client = null;
        }

        #endregion

        #region IChannelFactory

        public async Task<T> GetServiceAsync<T>()
            where T : class
        {
            await EnsureConnectedAsync();

            if (Client == null)
                throw new InvalidOperationException("Client is not connected");

            return Client.GetService<T>();
        }

        public async Task ReconnectAsync()
        {
            await m_gate.WaitAsync();
            try
            {
                await DisposeClientAsync();
                Client = await CreateClientAsync();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to reconnect");
            }
            finally
            {
                m_gate.Release();
            }
        }

        #endregion
        
        #region Event Handlers

        private async void OnAuthenticationProviderChanged(Task<AuthenticationState> task)
        {
            try
            {
                var state = await task;
                if (state.User?.Identity?.IsAuthenticated == true)
                {
                    await ReconnectAsync();
                }
                else
                {
                    await m_gate.WaitAsync();
                    try
                    {
                        await DisposeClientAsync();
                    }
                    finally
                    {
                        m_gate.Release();
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to process authentication status change");
            }
        }

        #endregion

        #region IAsyncDisposable

        public async ValueTask DisposeAsync()
        {
            if (IsDisposed) 
                return;
            
            IsDisposed = true;
            
            AuthenticationProvider.AuthenticationStateChanged -= OnAuthenticationProviderChanged;
            
            await DisposeClientAsync();
            
            m_gate.Dispose();
        }

        #endregion

        #region Properties
        
        private WitClient? Client { get; set; }
        
        private bool IsDisposed { get; set; }

        private NavigationManager NavigationManager { get; }
        
        private AuthenticationStateProvider AuthenticationProvider { get; }
        
        private EncryptorClientWeb Encryptor { get; }
        
        private ChannelTokenProvider TokenProvider { get; }
        
        private ChannelFactoryOptions Options { get; }

        private ILogger<ChannelFactory> Logger { get; }

        #endregion
    }
}
