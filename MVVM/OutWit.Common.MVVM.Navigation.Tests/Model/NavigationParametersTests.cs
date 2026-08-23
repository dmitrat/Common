using NUnit.Framework;
using OutWit.Common.MVVM.Navigation.Model;

namespace OutWit.Common.MVVM.Navigation.Tests.Model
{
    [TestFixture]
    public class NavigationParametersTests
    {
        #region Is Tests

        [Test]
        public void IsReturnsTrueForEqualSetsRegardlessOfOrderTest()
        {
            var left = new NavigationParameters(("id", 5), ("name", "a"));
            var right = new NavigationParameters(("name", "a"), ("id", 5));

            Assert.That(left.Is(right), Is.True);
            Assert.That(right.Is(left), Is.True);
        }

        [Test]
        public void IsReturnsFalseForDifferentValuesTest()
        {
            var left = new NavigationParameters(("id", 5));
            var right = new NavigationParameters(("id", 6));

            Assert.That(left.Is(right), Is.False);
        }

        [Test]
        public void IsReturnsFalseForDifferentKeysTest()
        {
            var left = new NavigationParameters(("id", 5));
            var right = new NavigationParameters(("id", 5), ("extra", 1));

            Assert.That(left.Is(right), Is.False);
            Assert.That(right.Is(left), Is.False);
        }

        [Test]
        public void IsComparesModelBaseValuesByIsTest()
        {
            var left = new NavigationParameters(("route", new NavigationRoute("a", typeof(string))));
            var right = new NavigationParameters(("route", new NavigationRoute("a", typeof(string))));

            Assert.That(left.Is(right), Is.True);
        }

        [Test]
        public void IsComparesDoublesWithToleranceTest()
        {
            var left = new NavigationParameters(("x", 1.0));
            var right = new NavigationParameters(("x", 1.0 + 1e-12));

            Assert.That(left.Is(right), Is.True);
        }

        [Test]
        public void IsTreatsNullValuesAsEqualTest()
        {
            var left = new NavigationParameters(("x", null));
            var right = new NavigationParameters(("x", null));

            Assert.That(left.Is(right), Is.True);
        }

        [Test]
        public void EmptyIsEmptyTest()
        {
            Assert.That(NavigationParameters.EMPTY.Count, Is.EqualTo(0));
            Assert.That(NavigationParameters.EMPTY.Is(new NavigationParameters()), Is.True);
        }

        #endregion

        #region Immutability Tests

        [Test]
        public void WithDoesNotModifyOriginalTest()
        {
            var original = new NavigationParameters(("id", 5));

            var modified = original.With("name", "a");

            Assert.That(original.Count, Is.EqualTo(1));
            Assert.That(original.Contains("name"), Is.False);
            Assert.That(modified.Count, Is.EqualTo(2));
            Assert.That(modified.Get<string>("name"), Is.EqualTo("a"));
        }

        [Test]
        public void WithReplacesExistingValueTest()
        {
            var original = new NavigationParameters(("id", 5));

            var modified = original.With("id", 6);

            Assert.That(original.Get<int>("id"), Is.EqualTo(5));
            Assert.That(modified.Get<int>("id"), Is.EqualTo(6));
        }

        [Test]
        public void WithoutDoesNotModifyOriginalTest()
        {
            var original = new NavigationParameters(("id", 5), ("name", "a"));

            var modified = original.Without("name");

            Assert.That(original.Count, Is.EqualTo(2));
            Assert.That(modified.Count, Is.EqualTo(1));
            Assert.That(modified.Contains("name"), Is.False);
        }

        [Test]
        public void WithoutReturnsSameInstanceWhenKeyIsAbsentTest()
        {
            var original = new NavigationParameters(("id", 5));

            Assert.That(original.Without("missing"), Is.SameAs(original));
        }

        [Test]
        public void CloneIsEqualButNotSameTest()
        {
            var original = new NavigationParameters(("id", 5));

            var clone = (NavigationParameters)original.Clone();

            Assert.That(clone, Is.Not.SameAs(original));
            Assert.That(clone.Is(original), Is.True);
        }

        #endregion

        #region Access Tests

        [Test]
        public void TryGetReturnsFalseForWrongTypeTest()
        {
            var parameters = new NavigationParameters(("id", 5));

            Assert.That(parameters.TryGet<string>("id", out _), Is.False);
            Assert.That(parameters.TryGet<int>("id", out var value), Is.True);
            Assert.That(value, Is.EqualTo(5));
        }

        [Test]
        public void GetReturnsDefaultWhenAbsentTest()
        {
            var parameters = new NavigationParameters();

            Assert.That(parameters.Get("id", 42), Is.EqualTo(42));
            Assert.That(parameters.Get<string>("name"), Is.Null);
        }

        [Test]
        public void IndexerReturnsNullWhenAbsentTest()
        {
            var parameters = new NavigationParameters(("id", 5));

            Assert.That(parameters["id"], Is.EqualTo(5));
            Assert.That(parameters["missing"], Is.Null);
        }

        [Test]
        public void EnumerationYieldsAllPairsTest()
        {
            var parameters = new NavigationParameters(("a", 1), ("b", 2));

            Assert.That(parameters, Has.Count.EqualTo(2));
            Assert.That(parameters.Keys, Is.EquivalentTo(new[] { "a", "b" }));
        }

        [Test]
        public void RepeatedKeyKeepsLastValueTest()
        {
            var parameters = new NavigationParameters(("a", 1), ("a", 2));

            Assert.That(parameters.Count, Is.EqualTo(1));
            Assert.That(parameters.Get<int>("a"), Is.EqualTo(2));
        }

        #endregion
    }
}
