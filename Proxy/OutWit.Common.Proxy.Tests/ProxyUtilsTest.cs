using OutWit.Common.Proxy.Tests.Mock;
using OutWit.Common.Proxy.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OutWit.Common.Proxy.Tests
{
    [TestFixture]
    public class ProxyUtilsTest
    {
        [Test]
        public void GetParametersTypesReturnsCorrectTypesTest()
        {
            // Arrange
            var invocation = new MockInvocation
            {
                ParametersTypes = new[] { "System.Int32", "System.String" }
            };

            // Act
            var types = invocation.GetParametersTypes();

            // Assert
            Assert.That(types, Is.Not.Null);
            Assert.That(types.Length, Is.EqualTo(2));
            Assert.That(types[0], Is.EqualTo(typeof(int)));
            Assert.That(types[1], Is.EqualTo(typeof(string)));
        }

        [Test]
        public void GetParametersTypesReturnsEmptyArrayForNullInputTest()
        {
            // Arrange
            var invocation = new MockInvocation { ParametersTypes = null };

            // Act
            var types = invocation.GetParametersTypes();

            // Assert
            Assert.That(types, Is.Not.Null);
            Assert.That(types.Length, Is.EqualTo(0));
        }

        [Test]
        public void GetReturnTypeReturnsCorrectTypeTest()
        {
            // Arrange
            var invocation = new MockInvocation { ReturnType = typeof(Guid).AssemblyQualifiedName };

            // Act
            var type = invocation.GetReturnType();

            // Assert
            Assert.That(type, Is.EqualTo(typeof(Guid)));
        }

        [Test]
        public void GetReturnTypeReturnsVoidForEmptyStringTest()
        {
            // Arrange
            var invocation = new MockInvocation { ReturnType = "" };

            // Act
            var type = invocation.GetReturnType();

            // Assert
            Assert.That(type, Is.EqualTo(typeof(void)));
        }

        [Test]
        public void TypeStringTypeStringReturnsAssemblyQualifiedNameTest()
        {
            // Arrange
            var type = typeof(string);

            // Act
            var typeString = type.TypeString();

            // Assert
            Assert.That(typeString, Is.EqualTo(type.AssemblyQualifiedName));
        }

        [Test]
        public void ResolveTypeAcceptsBareFacadeAndQualifiedNamesTest()
        {
            // The forms a generated proxy may carry: unqualified core-library names, names
            // qualified with a reference facade (System.Runtime / mscorlib), and nested generic
            // arguments in either form. All resolve to the same runtime types.
            Assert.Multiple(() =>
            {
                Assert.That(ProxyUtils.ResolveType("System.String"), Is.EqualTo(typeof(string)));
                Assert.That(ProxyUtils.ResolveType("System.String, System.Runtime"), Is.EqualTo(typeof(string)));
                Assert.That(ProxyUtils.ResolveType("System.Guid, mscorlib"), Is.EqualTo(typeof(Guid)));
                Assert.That(ProxyUtils.ResolveType("System.Collections.Generic.List`1[[System.String]]"), Is.EqualTo(typeof(List<string>)));
                Assert.That(ProxyUtils.ResolveType("System.Collections.Generic.List`1[[System.String, System.Runtime]], System.Runtime"), Is.EqualTo(typeof(List<string>)));
                Assert.That(ProxyUtils.ResolveType(typeof(Task<byte[]>).AssemblyQualifiedName), Is.EqualTo(typeof(Task<byte[]>)));
                Assert.That(ProxyUtils.ResolveType("No.Such.Type, No.Such.Assembly"), Is.Null);
                Assert.That(ProxyUtils.ResolveType(""), Is.Null);
                Assert.That(ProxyUtils.ResolveType(null), Is.Null);
            });
        }
    }
}
