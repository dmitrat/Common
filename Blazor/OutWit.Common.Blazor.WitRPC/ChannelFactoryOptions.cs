namespace OutWit.Common.Blazor.WitRPC
{
    /// <summary>
    /// Configuration options for ChannelFactory.
    /// </summary>
    public sealed class ChannelFactoryOptions
    {
        /// <summary>
        /// WebSocket API path (relative to base URL).
        /// Default: "api"
        /// </summary>
        public string ApiPath { get; set; } = "api";

        /// <summary>
        /// Connection and request timeout in seconds.
        /// Default: 10 seconds
        /// </summary>
        public int TimeoutSeconds { get; set; } = 10;
    }
}
