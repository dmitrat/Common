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

        [Test]
        public void GeneratesGenericMethodTest()
        {
            // Arrange
            var source = """
                        using OutWit.Common.Proxy.Attributes;
                        namespace MyTest
                        {
                            [ProxyTarget]
                            public interface IService5
                            {
                                T Echo<T>(int number, T value);
                            }
                        }
                        """;
            // Act
            var generatedCode = RunGenerator(source, out var diagnostics);

            // Assert
            Assert.That(diagnostics, Is.Empty);
            StringAssert.Contains("public T Echo<T>(int number, T value)", generatedCode);
            StringAssert.Contains("GenericArguments = new string[] { typeof(T).AssemblyQualifiedName! }", generatedCode);
            // The closed parameter keeps the literal format, the open one is resolved at run time.
            StringAssert.Contains("typeof(T).AssemblyQualifiedName!", generatedCode);
        }

        [Test]
        public void GeneratesAsyncGenericMethodWithNestedResultTest()
        {
            // Arrange: mirrors the OutWit.Cloud.Contracts IApiChannel shape —
            // Task<Result<TResult?>> with a default parameter value.
            var source = """
                        using System;
                        using System.Threading.Tasks;
                        using OutWit.Common.Proxy.Attributes;
                        namespace MyTest
                        {
                            public class Result<T> { public T Value; }

                            [ProxyTarget]
                            public interface IService6
                            {
                                Task<Result<TResult?>> GetJobResultAsync<TResult>(Guid jobId, string resultVariable = "result");
                            }
                        }
                        """;
            // Act
            var generatedCode = RunGenerator(source, out var diagnostics);

            // Assert
            Assert.That(diagnostics, Is.Empty);
            StringAssert.Contains("GetJobResultAsync<TResult>(", generatedCode);
            StringAssert.Contains("GenericArguments = new string[] { typeof(TResult).AssemblyQualifiedName! }", generatedCode);
            // The nullable annotation must not leak into typeof(...).
            StringAssert.Contains("typeof(global::MyTest.Result<TResult>).AssemblyQualifiedName!", generatedCode);
        }

        [Test]
        public void GeneratesGenericMethodWithConstraintsTest()
        {
            // Arrange
            var source = """
                        using System.Threading.Tasks;
                        using OutWit.Common.Proxy.Attributes;
                        namespace MyTest
                        {
                            [ProxyTarget]
                            public interface IService7
                            {
                                Task<T> CreateAsync<T>() where T : class, new();
                            }
                        }
                        """;
            // Act
            var generatedCode = RunGenerator(source, out var diagnostics);

            // Assert
            Assert.That(diagnostics, Is.Empty);
            StringAssert.Contains("CreateAsync<T>() where T : class, new()", generatedCode);
        }

        [Test]
        public void NonGenericMethodsKeepLiteralTypeStringsTest()
        {
            // Arrange: guards the historical emission — closed types must keep the
            // compile-time literal format, not switch to typeof(...) expressions.
            var source = """
                        using OutWit.Common.Proxy.Attributes;
                        namespace MyTest
                        {
                            [ProxyTarget]
                            public interface IService8
                            {
                                string RequestData(string message);
                            }
                        }
                        """;
            // Act
            var generatedCode = RunGenerator(source, out var diagnostics);

            // Assert
            Assert.That(diagnostics, Is.Empty);
            StringAssert.Contains("ParametersTypes = new string[] { \"System.String, ", generatedCode);
            StringAssert.DoesNotContain("typeof(", generatedCode);
        }
    }
}
