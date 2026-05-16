using OutWit.Common.Logging.NewRelic.Model;
using OutWit.Common.Logging.Query.Model;
using OutWit.Common.NUnit;
using System;
using System.Collections.Generic;
using System.Text;
using OutWit.Common.Utils;

namespace OutWit.Common.Logging.NewRelic.Tests.Model
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
            Assert.That(options.BaseFilters, Is.Empty);
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
                MaxPageSize = 500,
                BaseFilters = new[] { LogFilter.Eq("service.name", "WitIdentity") }
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
            // BaseFilters cloned by value, not by reference.
            Assert.That(clone.BaseFilters, Is.Not.SameAs(options.BaseFilters));
            Assert.That(clone.BaseFilters.Length, Is.EqualTo(1));
            Assert.That(clone.BaseFilters[0].Attribute, Is.EqualTo("service.name"));
            Assert.That(clone.BaseFilters[0].Values, Is.EqualTo(new[] { "WitIdentity" }));
        }

        [Test]
        public void IsTreatsBaseFiltersAsValueTest()
        {
            // Arrange
            var a = new NewRelicClientOptions
            {
                ApiKey = "key",
                AccountId = 1,
                BaseFilters = new[] { LogFilter.Eq("service.name", "WitIdentity") }
            };
            var sameContent = new NewRelicClientOptions
            {
                ApiKey = "key",
                AccountId = 1,
                BaseFilters = new[] { LogFilter.Eq("service.name", "WitIdentity") }
            };
            var differentContent = new NewRelicClientOptions
            {
                ApiKey = "key",
                AccountId = 1,
                BaseFilters = new[] { LogFilter.Eq("service.name", "WitCloud") }
            };

            // Assert
            Assert.That(a, Was.EqualTo(sameContent));
            Assert.That(a, Was.Not.EqualTo(differentContent));
        }
    }
}
