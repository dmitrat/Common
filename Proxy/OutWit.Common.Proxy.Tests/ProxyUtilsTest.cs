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
    }
}
