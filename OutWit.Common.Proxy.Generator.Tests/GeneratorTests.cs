using NUnit.Framework.Legacy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OutWit.Common.Proxy.Generator.Tests
{
    [TestFixture]
    public class GeneratorTests : GeneratorTestBase
    {
        [Test]
        public void GeneratesProxyWithDefaultNameTest()
        {
            // Arrange
            var source = """
                        using OutWit.Common.Proxy.Attributes;
                        namespace MyTest
                        {
                            [ProxyTarget]
                            public interface IService1 { void DoWork(); }
                        }
                        """;
            // Act
            var generatedCode = RunGenerator(source, out var diagnostics);

            // Assert
            Assert.That(diagnostics, Is.Empty);
            StringAssert.Contains("public class IService1Proxy : MyTest.IService1", generatedCode);
            StringAssert.Contains("MethodName = \"DoWork\"", generatedCode);
        }

        [Test]
        public void GeneratesProxyWithCustomNameTest()
        {
            // Arrange
            var source = """
                        using OutWit.Common.Proxy.Attributes;
                        namespace MyTest
                        {
                            [ProxyTarget("MyCoolProxy")]
                            public interface IService2 { }
                        }
                        """;
            // Act
            var generatedCode = RunGenerator(source, out var diagnostics);

            // Assert
            Assert.That(diagnostics, Is.Empty);
            StringAssert.Contains("public class MyCoolProxy : MyTest.IService2", generatedCode);
        }

        [Test]
        public void GeneratesPropertyWithGetterAndSetterTest()
        {
            // Arrange
            var source = """
                        using OutWit.Common.Proxy.Attributes;
                        namespace MyTest
                        {
                            [ProxyTarget]
                            public interface IWithProperty { string Name { get; set; } }
                        }
                        """;
            // Act
            var generatedCode = RunGenerator(source, out var diagnostics);

            // Assert
            Assert.That(diagnostics, Is.Empty);
            StringAssert.Contains("public string Name", generatedCode);
            // Check for getter
            StringAssert.Contains("MethodName = \"get_Name\"", generatedCode);
            // Check for setter
            StringAssert.Contains("MethodName = \"set_Name\"", generatedCode);
            StringAssert.Contains("Parameters = new object[] { value }", generatedCode);
        }

        [Test]
        public void GeneratesAsyncMethodWithResultTest()
        {
            // Arrange
            var source = """
                        using OutWit.Common.Proxy.Attributes;
                        using System.Threading.Tasks;
                        namespace MyTest
                        {
                            [ProxyTarget]
                            public interface IWithAsync
                            {
                                Task<int> GetValueAsync(string key);
                            }
                        }
                        """;
            // Act
            var generatedCode = RunGenerator(source, out var diagnostics);

            // Assert
            Assert.That(diagnostics, Is.Empty);
            StringAssert.Contains("public System.Threading.Tasks.Task<int> GetValueAsync(string key)", generatedCode);
            StringAssert.Contains("ReturnsTaskWithResult = true", generatedCode);
            StringAssert.Contains("TaskResultType = \"System.Int32", generatedCode);
            StringAssert.Contains("return ((System.Threading.Tasks.Task<object>)invocation.ReturnValue).ContinueWith(x => (int)x.Result);", generatedCode);
        }

        [Test]
        public void GeneratesEventTest()
        {
            // Arrange
            var source = """
                        using OutWit.Common.Proxy.Attributes;
                        using System;
                        namespace MyTest
                        {
                            [ProxyTarget]
                            public interface IWithEvent
                            {
                                event EventHandler MyEvent;
                            }
                        }
                        """;
            // Act
            var generatedCode = RunGenerator(source, out var diagnostics);

            // Assert
            Assert.That(diagnostics, Is.Empty);
            StringAssert.Contains("public event System.EventHandler MyEvent", generatedCode);
            // Check for adder
            StringAssert.Contains("MethodName = \"add_MyEvent\"", generatedCode);
            StringAssert.Contains("_MyEvent += value;", generatedCode);
            // Check for remover
            StringAssert.Contains("MethodName = \"remove_MyEvent\"", generatedCode);
            StringAssert.Contains("_MyEvent -= value;", generatedCode);
        }
    }
}
