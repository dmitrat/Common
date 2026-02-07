using System;
using OutWit.Common.Abstract;
using OutWit.Common.Values;

namespace OutWit.Common.NewRelic.Model
{
    public sealed class NewRelicClientOptions : ModelBase
    {
        #region Constants

        private const string DEFAULT_ENDPOINT = "https://api.newrelic.com/graphql";

        private const int DEFAULT_PAGE_SIZE = 100;

        private const int DEFAULT_MAX_PAGE_SIZE = 1000;

        #endregion

        #region Model Base

        public override bool Is(ModelBase modelBase, double tolerance = 1E-07)
        {
            if (modelBase is not NewRelicClientOptions options)
                return false;

            return ApiKey.Is(options.ApiKey)
                && AccountId.Is(options.AccountId)
                && Endpoint.Is(options.Endpoint)
                && DefaultPageSize.Is(options.DefaultPageSize)
                && MaxPageSize.Is(options.MaxPageSize);
        }

        public override NewRelicClientOptions Clone()
        {
            return new NewRelicClientOptions
            {
                ApiKey = ApiKey,
                AccountId = AccountId,
                Endpoint = Endpoint,
                DefaultPageSize = DefaultPageSize,
                MaxPageSize = MaxPageSize
            };
        }

        #endregion

        #region Properties


        /// <summary>
        /// User API key for NerdGraph.
        /// </summary>
#if NET7_0_OR_GREATER
        public required string ApiKey { get; set; }
#else
        public string ApiKey { get; set; }
#endif

        /// <summary>
        /// New Relic account id.
        /// </summary>
#if NET7_0_OR_GREATER
        public required int AccountId { get; set; }
#else
        public int AccountId { get; set; }
#endif

        /// <summary>
        /// GraphQL endpoint.
        /// </summary>
        public string Endpoint { get; set; } = DEFAULT_ENDPOINT;

        /// <summary>
        /// Gets or sets the default number of items to include in a single page of results.
        /// </summary>
        public int DefaultPageSize { get; set; } = DEFAULT_PAGE_SIZE;

        /// <summary>
        /// Gets or sets the maximum number of items to include in a single page of results.
        /// </summary>
        public int MaxPageSize { get; set; } = DEFAULT_MAX_PAGE_SIZE;

        #endregion
    }
}
