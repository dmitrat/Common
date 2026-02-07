using Microsoft.Extensions.Logging;
using OutWit.Common.NewRelic.Model;
using OutWit.Common.NewRelic.Requests;
using OutWit.Common.NewRelic.Response;
using OutWit.Common.Rest;
using OutWit.Common.Rest.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace OutWit.Common.NewRelic
{
    public sealed class NewRelicHttpClient : RestClientBase
    {
        #region Constants

        private const string AUTHORIZATION_SCHEME = "API-Key";

        #endregion

        #region Constructors

        public NewRelicHttpClient(NewRelicClientOptions options)
        {
            Options = options ?? throw new ArgumentNullException(nameof(options));

            HttpClient.BaseAddress = new Uri(options.Endpoint);

            this.WithHeader(AUTHORIZATION_SCHEME, options.ApiKey);
        }

        public NewRelicHttpClient(HttpClient httpClient, NewRelicClientOptions options)
            : base(httpClient)
        {
            Options = options ?? throw new ArgumentNullException(nameof(options));

            HttpClient.BaseAddress ??= new Uri(options.Endpoint);

            this.WithHeader(AUTHORIZATION_SCHEME, options.ApiKey);
        }

        #endregion

        #region Functions

        /// <summary>
        /// Executes a NRQL query via NerdGraph and returns the results.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when GraphQL returns errors</exception>
        internal async Task<List<Dictionary<string, JsonElement>>> PostNrqlAsync(string nrql,
            CancellationToken cancellationToken = default)
        {
            var response = await PostAsync<NerdGraphResponse>(new NrqlRequest(Options.AccountId, nrql))
                .ConfigureAwait(false);

            // Check for GraphQL errors
            if (response.Errors is { Length: > 0 })
            {
                var errorMessages = string.Join(", ", response.Errors.Select(e => e.Message));
                throw new InvalidOperationException($"New Relic GraphQL query failed: {errorMessages}");
            }

            return response.Data?.Actor?.Account?.Result?.Results
                   ?? new List<Dictionary<string, JsonElement>>();
        }

        internal NewRelicLogQuery Validate(NewRelicLogQuery query)
        {
            var rawPageSize = query.PageSize ?? Options.DefaultPageSize;
            query.PageSize = Math.Clamp(rawPageSize, 1, Options.MaxPageSize);

            return query;
        }
        #endregion

        #region Properties

        private NewRelicClientOptions Options { get; }

        #endregion
    }
}
