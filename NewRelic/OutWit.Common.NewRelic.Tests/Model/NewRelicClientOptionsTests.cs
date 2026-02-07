using OutWit.Common.NewRelic.Model;
using OutWit.Common.NUnit;
using System;
using System.Collections.Generic;
using System.Text;
using OutWit.Common.Utils;

namespace OutWit.Common.NewRelic.Tests.Model
{
    [TestFixture]
    public class NewRelicClientOptionsTests
    {
        [Test]
        public void ConstructorTest()
        {
            // Arrange & Act
            var options = new NewRelicClientOptions
            {
                ApiKey = "test-key",
                AccountId = 12345
            };

            // Assert
            Assert.That(options.ApiKey, Is.EqualTo("test-key"));
            Assert.That(options.AccountId, Is.EqualTo(12345));
            // Check defaults
            Assert.That(options.Endpoint, Is.EqualTo("https://api.newrelic.com/graphql"));
            Assert.That(options.DefaultPageSize, Is.EqualTo(100));
            Assert.That(options.MaxPageSize, Is.EqualTo(1000));
        }

        [Test]
        public void IsTest()
        {
            // Arrange
            var options = new NewRelicClientOptions
            {
                ApiKey = "key",
                AccountId = 1,
                Endpoint = "endpoint",
                DefaultPageSize = 50,
                MaxPageSize = 500
            };

            // Assert
            Assert.That(options, Was.EqualTo(options.Clone()));
            Assert.That(options, Was.Not.EqualTo(options.With(x => x.ApiKey, "key2")));
            Assert.That(options, Was.Not.EqualTo(options.With(x => x.AccountId, 2)));
            Assert.That(options, Was.Not.EqualTo(options.With(x => x.Endpoint, "endpoint2")));
            Assert.That(options, Was.Not.EqualTo(options.With(x => x.DefaultPageSize, 51)));
            Assert.That(options, Was.Not.EqualTo(options.With(x => x.MaxPageSize, 501)));
        }

        [Test]
        public void CloneTest()
        {
            // Arrange
            var options = new NewRelicClientOptions
            {
                ApiKey = "key",
                AccountId = 1,
                Endpoint = "endpoint",
                DefaultPageSize = 50,
                MaxPageSize = 500
            };

            // Act
            var clone = options.Clone() as NewRelicClientOptions;

            // Assert
            Assert.That(clone, Is.Not.Null);
            Assert.That(clone, Is.Not.SameAs(options));
            Assert.That(clone.ApiKey, Is.EqualTo(options.ApiKey));
            Assert.That(clone.AccountId, Is.EqualTo(options.AccountId));
            Assert.That(clone.Endpoint, Is.EqualTo(options.Endpoint));
            Assert.That(clone.DefaultPageSize, Is.EqualTo(options.DefaultPageSize));
            Assert.That(clone.MaxPageSize, Is.EqualTo(options.MaxPageSize));
        }
    }
}
