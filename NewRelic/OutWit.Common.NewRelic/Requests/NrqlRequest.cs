using OutWit.Common.Rest.Interfaces;
using OutWit.Common.Rest.Utils;
using System;
using System.Net.Http;
using System.Text;

namespace OutWit.Common.NewRelic.Requests
{
    /// <summary>
    /// New Relic NerdGraph NRQL request.
    /// Wraps NRQL into a GraphQL query and builds JSON POST body.
    /// Designed to be used with RestClientBase.PostAsync.
    /// </summary>
    internal sealed class NrqlRequest : IRequestPost
    {
        #region Constructors

        /// <summary>
        /// Creates a new NRQL GraphQL request to the New Relic NerdGraph endpoint.
        /// </summary>
        /// <param name="accountId">New Relic account id.</param>
        /// <param name="nrql">Raw NRQL query (e.g. "SELECT * FROM Log SINCE 1 hour ago").</param>
        /// <param name="relativePath">
        /// Relative URL for the GraphQL endpoint.
        /// Usually empty string when HttpClient.BaseAddress is already set to .../graphql.
        /// </param>
        public NrqlRequest(int accountId, string nrql, string relativePath = "")
        {
            AccountId = accountId;
            Nrql = nrql ?? throw new ArgumentNullException(nameof(nrql));
            RelativePath = relativePath ?? string.Empty;
        }

        #endregion

        #region IRequest

        public Uri Build()
        {
            return new Uri(RelativePath, UriKind.Relative);
        }

        #endregion

        #region IRequestPost

        public HttpContent? BuildContent()
        {
            var gql = BuildGraphQlQuery();
            var body = new { query = gql };

            return body.JsonContent();
        }

        #endregion

        #region Helpers

        private string BuildGraphQlQuery()
        {
            var escapedNrql = EscapeForGraphQl(Nrql);
            var sb = new StringBuilder();

            sb.AppendLine("{");
            sb.AppendLine("  actor {");
            sb.Append("    account(id: ");
            sb.Append(AccountId);
            sb.AppendLine(") {");
            sb.Append("      Result: nrql(query: \"");
            sb.Append(escapedNrql);
            sb.AppendLine("\") {");
            sb.AppendLine("        Results");
            sb.AppendLine("      }");
            sb.AppendLine("    }");
            sb.AppendLine("  }");
            sb.AppendLine("}");

            return sb.ToString();
        }

        private static string EscapeForGraphQl(string nrql)
        {
            return nrql
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"");
        }

        #endregion

        #region Properties

        /// <summary>New Relic account id.</summary>
        public int AccountId { get; }

        /// <summary>Raw NRQL query text.</summary>
        public string Nrql { get; }

        /// <summary>Relative path that will be combined with HttpClient.BaseAddress.</summary>
        public string RelativePath { get; }

        #endregion
    }
}
