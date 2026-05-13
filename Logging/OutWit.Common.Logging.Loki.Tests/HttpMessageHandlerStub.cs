using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace OutWit.Common.Logging.Loki.Tests
{
    /// <summary>
    /// Minimal <see cref="HttpMessageHandler"/> that returns pre-canned JSON for a
    /// queue of expected requests — used to drive <see cref="LokiHttpClient"/>
    /// through real HttpClient internals without hitting any network.
    /// </summary>
    internal sealed class HttpMessageHandlerStub : HttpMessageHandler
    {
        #region Fields

        private readonly Queue<(Func<HttpRequestMessage, bool> Predicate, string Body, HttpStatusCode Status)> m_responses = new();

        #endregion

        #region Functions

        public HttpMessageHandlerStub EnqueueResponse(string body,
            HttpStatusCode status = HttpStatusCode.OK,
            Func<HttpRequestMessage, bool>? predicate = null)
        {
            m_responses.Enqueue((predicate ?? (_ => true), body, status));
            return this;
        }

        public List<HttpRequestMessage> Received { get; } = new();

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Received.Add(request);

            if (m_responses.Count == 0)
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    Content = new StringContent("No canned response queued.")
                });
            }

            var (predicate, body, status) = m_responses.Dequeue();
            if (!predicate(request))
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new StringContent($"Request did not match: {request.RequestUri}")
                });
            }

            return Task.FromResult(new HttpResponseMessage(status)
            {
                Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json")
            });
        }

        #endregion
    }
}
