using OutWit.Common.Rest.Tests.Mock;
using OutWit.Common.Rest.Tests.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using OutWit.Common.Rest.Exceptions;
using OutWit.Common.Rest.Utils;

namespace OutWit.Common.Rest.Tests
{
    [TestFixture]
    public class RestClientTests
    {
        private MockHttpMessageHandler _mockHandler = null!;
        private HttpClient _httpClient = null!;
        private RestClientBase _restClient = null!;

        [SetUp]
        public void Setup()
        {
            _mockHandler = new MockHttpMessageHandler();
            _httpClient = new HttpClient(_mockHandler);

            // Assuming RestClientBuilder.Create can accept an HttpClient
            // This is the improved design we discussed earlier
            _restClient = RestClientBuilder.Create(_httpClient);
        }

        [TearDown]
        public void Teardown()
        {
            _mockHandler.Dispose();
            _httpClient.Dispose();
            _restClient.Dispose();
        }

        [Test]
        public void WithBearerAuthorizationHeaderTest()
        {
            // Arrange
            const string token = "my-secret-token";

            // Act
            _restClient.WithBearer(token);

            // Assert
            var authHeader = _httpClient.DefaultRequestHeaders.Authorization;
            Assert.That(authHeader, Is.Not.Null);
            Assert.That(authHeader.Scheme, Is.EqualTo("Bearer"));
            Assert.That(authHeader.Parameter, Is.EqualTo(token));
        }

        [Test]
        public void WithCustomHeaderTest()
        {
            // Arrange
            const string headerName = "X-Custom-Header";
            const string headerValue = "my-custom-value";

            // Act
            _restClient.WithHeader(headerName, headerValue);

            // Assert
            Assert.That(_httpClient.DefaultRequestHeaders.Contains(headerName), Is.True);
            var values = _httpClient.DefaultRequestHeaders.GetValues(headerName);
            Assert.That(values, Has.Member(headerValue));
        }

        [Test]
        public async Task GetAsyncSuccessDeserializesResponseTest()
        {
            // Arrange
            var expectedDto = new TestDto { Id = 1, Name = "Test Item" };
            var jsonResponse = "{ \"id\": 1, \"name\": \"Test Item\" }";
            _mockHandler.Response = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(jsonResponse)
            };

            // Act
            var result = await _restClient.GetAsync<TestDto>("http://test.api/items/1");

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Id, Is.EqualTo(expectedDto.Id));
            Assert.That(result.Name, Is.EqualTo(expectedDto.Name));
            Assert.That(_mockHandler.Request?.Method, Is.EqualTo(HttpMethod.Get));
            Assert.That(_mockHandler.Request?.RequestUri?.ToString(), Is.EqualTo("http://test.api/items/1"));
        }

        [Test]
        public void GetAsyncThrowsRestClientExceptionOnFailureTest()
        {
            // Arrange
            _mockHandler.Response = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.NotFound,
                Content = new StringContent("{\"error\":\"Item not found\"}")
            };

            // Act & Assert
            var ex = Assert.ThrowsAsync<RestClientException>(async () =>
                await _restClient.GetAsync<TestDto>("http://test.api/items/99"));

            Assert.That(ex, Is.Not.Null);
            Assert.That(ex.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            Assert.That(ex.Content, Is.EqualTo("{\"error\":\"Item not found\"}"));
        }

        [Test]
        public async Task PostAsyncSuccessSerializesRequestAndDeserializesResponseTest()
        {
            // Arrange
            var requestDto = new TestDto { Name = "New Item" };
            var responseDto = new TestDto { Id = 10, Name = "New Item" };
            var jsonResponse = "{ \"id\": 10, \"name\": \"New Item\" }";

            _mockHandler.Response = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.Created, // Using 201 Created to test IsSuccessStatusCode
                Content = new StringContent(jsonResponse)
            };

            // Act
            var result = await _restClient.PostAsync<TestDto>("http://test.api/items", requestDto.JsonContent());

            // Assert
            // Check response deserialization
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Id, Is.EqualTo(responseDto.Id));

            // Check request serialization
            Assert.That(_mockHandler.Request?.Method, Is.EqualTo(HttpMethod.Post));
            var requestBody = await (_mockHandler.Request?.Content?.ReadAsStringAsync() ?? Task.FromResult(""));
            Assert.That(requestBody, Is.EqualTo("{\"Id\":0,\"Name\":\"New Item\"}"));
        }
    }
}
