using OutWit.Common.Utils;

namespace OutWit.Common.Proxy.Tests
{
    [TestFixture]
    public class ProxyInvocationTest
    {
        [Test]
        public void ConstructorTest()
        {
            var invocation = new ProxyInvocation();
            Assert.That(invocation.MethodName, Is.Null);
            Assert.That(invocation.Parameters, Is.Null);
            Assert.That(invocation.ParametersTypes, Is.Null);
            Assert.That(invocation.GenericArguments, Is.Null);
            Assert.That(invocation.HasReturnValue, Is.False);
            Assert.That(invocation.ReturnValue, Is.Null);
            Assert.That(invocation.ReturnType, Is.Null);
            Assert.That(invocation.ReturnsTask, Is.False);
            Assert.That(invocation.ReturnsTaskWithResult, Is.False);
            Assert.That(invocation.TaskResultType, Is.Null);
            
            invocation = new ProxyInvocation
            {
                MethodName = "TestMethod",
                Parameters = new object[] { 42, "hello" },
                ParametersTypes = new[] { "System.Int32", "System.String" },
                GenericArguments = new[] { "System.Guid" },
                HasReturnValue = true,
                ReturnValue = "world",
                ReturnType = "System.String",
                ReturnsTask = false,
                ReturnsTaskWithResult = false,
                TaskResultType = ""
            };

            Assert.That(invocation.MethodName, Is.EqualTo("TestMethod"));
            Assert.That(invocation.Parameters, Is.EqualTo(new object[] { 42, "hello" }));
            Assert.That(invocation.ParametersTypes, Is.EqualTo(new[] { "System.Int32", "System.String" }));
            Assert.That(invocation.GenericArguments, Is.EqualTo(new[] { "System.Guid" }));
            Assert.That(invocation.HasReturnValue, Is.True);
            Assert.That(invocation.ReturnValue, Is.EqualTo("world"));
            Assert.That(invocation.ReturnType, Is.EqualTo("System.String"));
            Assert.That(invocation.ReturnsTask, Is.False);
            Assert.That(invocation.ReturnsTaskWithResult, Is.False);
            Assert.That(invocation.TaskResultType, Is.EqualTo(""));
        }

        [Test]
        public void IsTest()
        {
            var invocation = new ProxyInvocation
            {
                MethodName = "TestMethod",
                Parameters = new object[] { 42, "hello" },
                ParametersTypes = new[] { "System.Int32", "System.String" },
                GenericArguments = new[] { "System.Guid" },
                HasReturnValue = true,
                ReturnValue = "world",
                ReturnType = "System.String",
                ReturnsTask = false,
                ReturnsTaskWithResult = false,
                TaskResultType = ""
            };

            Assert.That(invocation.Is(invocation.Clone()), Is.True);
            
            Assert.That(invocation.Is(invocation.With(x=>x.MethodName = "TestMethod2")), Is.False);
            Assert.That(invocation.Is(invocation.With(x => x.Parameters = new object[] { 43, "hello" })), Is.False);
            Assert.That(invocation.Is(invocation.With(x => x.ParametersTypes = new[] { "System.Int64" })), Is.False);
            Assert.That(invocation.Is(invocation.With(x => x.GenericArguments = new[] { "System.Int32" })), Is.False);
            Assert.That(invocation.Is(invocation.With(x => x.HasReturnValue = false)), Is.False);
            Assert.That(invocation.Is(invocation.With(x => x.ReturnValue = "world2")), Is.False);
            Assert.That(invocation.Is(invocation.With(x => x.ReturnType = "System.Int32")), Is.False);
            Assert.That(invocation.Is(invocation.With(x => x.ReturnsTask = true)), Is.False);
            Assert.That(invocation.Is(invocation.With(x => x.ReturnsTaskWithResult = true)), Is.False);
            Assert.That(invocation.Is(invocation.With(x => x.TaskResultType = "System.Int32")), Is.False);
        }

        [Test]
        public void CloneTest()
        {
            var invocation1 = new ProxyInvocation
            {
                MethodName = "TestMethod",
                Parameters = new object[] { 42, "hello" },
                ParametersTypes = new[] { "System.Int32", "System.String" },
                GenericArguments = new[] { "System.Guid" },
                HasReturnValue = true,
                ReturnValue = "world",
                ReturnType = "System.String",
                ReturnsTask = false,
                ReturnsTaskWithResult = false,
                TaskResultType = ""
            };
            
            var invocation2 = invocation1.Clone() as ProxyInvocation;

            Assert.That(invocation2.MethodName, Is.EqualTo("TestMethod"));
            Assert.That(invocation2.Parameters, Is.EqualTo(new object[] { 42, "hello" }));
            Assert.That(invocation2.ParametersTypes, Is.EqualTo(new[] { "System.Int32", "System.String" }));
            Assert.That(invocation2.GenericArguments, Is.EqualTo(new[] { "System.Guid" }));
            Assert.That(invocation2.HasReturnValue, Is.True);
            Assert.That(invocation2.ReturnValue, Is.EqualTo("world"));
            Assert.That(invocation2.ReturnType, Is.EqualTo("System.String"));
            Assert.That(invocation2.ReturnsTask, Is.False);
            Assert.That(invocation2.ReturnsTaskWithResult, Is.False);
            Assert.That(invocation2.TaskResultType, Is.EqualTo(""));
        }



    }
}