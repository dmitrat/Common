using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OutWit.Common.Proxy.Interfaces;

namespace OutWit.Common.Proxy.Tests.Mock
{
    internal class MockInvocation : IProxyInvocation
    {
        public string? MethodName { get; set; }
        public object[]? Parameters { get; set; }
        public string[]? ParametersTypes { get; set; }
        public string[]? GenericArguments { get; set; }
        public bool HasReturnValue { get; set; }
        public object? ReturnValue { get; set; }
        public string? ReturnType { get; set; }
        public bool ReturnsTask { get; set; }
        public bool ReturnsTaskWithResult { get; set; }
        public string? TaskResultType { get; } // Readonly in interface
    }
}
