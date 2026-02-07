using Microsoft.Extensions.DependencyInjection;
using OutWit.Common.Blazor.WitRPC.Encryption;

namespace OutWit.Common.Blazor.WitRPC
{
    /// <summary>
    /// Extension methods for registering WitRPC services.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds WitRPC channel factory and related services for Blazor WebAssembly.
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <param name="configure">Optional configuration action</param>
        /// <returns>Service collection for chaining</returns>
        /// <example>
        /// builder.Services.AddWitRpcChannel(options => 
        /// {
        ///     options.ApiPath = "api";
        ///     options.TimeoutSeconds = 15;
        /// });
        /// </example>
        public static IServiceCollection AddWitRpcChannel(
            this IServiceCollection services,
            Action<ChannelFactoryOptions>? configure = null)
        {
            var options = new ChannelFactoryOptions();
            configure?.Invoke(options);

            services.AddSingleton(options);
            services.AddScoped<EncryptorClientWeb>();
            services.AddScoped<ChannelTokenProvider>();
            services.AddScoped<IChannelFactory, ChannelFactory>();

            return services;
        }
    }
}
