using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework.Legacy;

namespace OutWit.Common.Proxy.Generator.Tests
{
    /// <summary>
    /// Guards the type strings the proxy emits into <c>ProxyInvocation</c>. The receiving
    /// side resolves every one of them with <c>Type.GetType</c>, so an unrenderable type
    /// used to degrade silently: an empty parameter entry made the server report the method
    /// as missing, and an empty task-result entry made the client receive null. Arrays hit
    /// exactly that hole (byte[] is an IArrayTypeSymbol, not an INamedTypeSymbol).
    /// </summary>
    [TestFixture]
    public class TypeStringGeneratorTests : GeneratorTestBase
    {
        #region Constants

        private const string HEADER = """
                        using System;
                        using System.Threading.Tasks;
                        using OutWit.Common.Proxy.Attributes;

                        namespace MyTest
                        {
                            public class Result<T> { public T Data { get; set; } }

                            public class Outer { public class Inner { } }

                        """;

        private const string FOOTER = """
                        }
                        """;

        #endregion

        #region Array Tests

        [Test]
        public void ByteArrayParameterRendersAssemblyQualifiedArrayNameTest()
        {
            var generatedCode = RunGeneratorForInterface("Task<int> UploadAsync(byte[] data, string fileName);");

            var parameters = ExtractParameterTypes(generatedCode);

            Assert.That(parameters, Has.Count.EqualTo(2));
            // Resolution is the oracle: the receiving side feeds these to Type.GetType, and
            // the assembly identity may be the reference one (System.Runtime) rather than the
            // runtime one, which resolves to the same Type.
            Assert.That(Type.GetType(parameters[0]), Is.EqualTo(typeof(byte[])));
            Assert.That(Type.GetType(parameters[1]), Is.EqualTo(typeof(string)));
        }

        [Test]
        public void ByteArrayTaskResultRendersAssemblyQualifiedArrayNameTest()
        {
            var generatedCode = RunGeneratorForInterface("Task<byte[]> DownloadAsync(Guid blobId);");

            Assert.That(Type.GetType(ExtractTaskResultType(generatedCode)), Is.EqualTo(typeof(byte[])));
        }

        [Test]
        public void ArrayInsideGenericArgumentKeepsInnerArrayTest()
        {
            // The regression that broke blob downloads: Result<byte[]> rendered as
            // "Result`1[[]]" because the inner array collapsed to an empty string.
            var generatedCode = RunGeneratorForInterface("Task<Result<byte[]>> DownloadAsync(Guid blobId);");

            var taskResultType = ExtractTaskResultType(generatedCode);

            StringAssert.StartsWith("MyTest.Result`1[[System.Byte[], ", taskResultType);
            StringAssert.DoesNotContain("[[]]", taskResultType);
        }

        [Test]
        public void JaggedAndMultiDimensionalArraysRenderRankBracketsTest()
        {
            var generatedCode = RunGeneratorForInterface("Task StoreAsync(byte[][] jagged, int[,] grid, long[][,] mixed);");

            var parameters = ExtractParameterTypes(generatedCode);

            Assert.Multiple(() =>
            {
                Assert.That(Type.GetType(parameters[0]), Is.EqualTo(typeof(byte[][])));
                Assert.That(Type.GetType(parameters[1]), Is.EqualTo(typeof(int[,])));
                // C# lists ranks outermost-first, reflection innermost-first: the C# type
                // long[][,] is named System.Int64[,][] in metadata. Both spellings must hold —
                // the signature has to compile and the type string has to resolve.
                Assert.That(Type.GetType(parameters[2]), Is.EqualTo(typeof(long[][,])));
                StringAssert.StartsWith("System.Int64[,][], ", parameters[2]);
            });

            StringAssert.Contains("StoreAsync(byte[][] jagged, int[,] grid, long[][,] mixed)", generatedCode);
        }

        [Test]
        public void ArrayOfContractTypeKeepsElementAssemblyTest()
        {
            var generatedCode = RunGeneratorForInterface("Task SendAsync(Result<int>[] items);");

            StringAssert.StartsWith("MyTest.Result`1[[System.Int32, ", ExtractParameterTypes(generatedCode)[0]);
            StringAssert.Contains("]][], TestAssembly", ExtractParameterTypes(generatedCode)[0]);
        }

        #endregion

        #region Shape Tests

        [Test]
        public void NestedTypeRendersWithPlusSeparatorTest()
        {
            var generatedCode = RunGeneratorForInterface("Task SendAsync(Outer.Inner value);");

            StringAssert.StartsWith("MyTest.Outer+Inner, ", ExtractParameterTypes(generatedCode)[0]);
        }

        [Test]
        public void GenericMethodArrayParameterFallsBackToTypeofTest()
        {
            // T[] is only known at run time, so the proxy must resolve it through typeof.
            var generatedCode = RunGeneratorForInterface("Task SendAsync<T>(T[] items) where T : class;");

            StringAssert.Contains("typeof(T[]).AssemblyQualifiedName!", generatedCode);
        }

        [Test]
        public void NoEmittedParameterOrResultTypeIsEmptyTest()
        {
            var generatedCode = RunGeneratorForInterface("""
                                    Task<int> UploadAsync(byte[] data, string fileName);
                                    Task<byte[]> DownloadAsync(Guid blobId);
                                    Task<Result<byte[]>> DownloadChunkAsync(Guid blobId, long offset, int length);
                                    Task AppendAsync(Guid uploadId, int chunkIndex, byte[] data);
                                    """);

            Assert.Multiple(() =>
            {
                Assert.That(ExtractParameterTypes(generatedCode), Has.None.Empty);
                Assert.That(ExtractAllTaskResultTypes(generatedCode), Has.None.Empty);
            });
        }

        #endregion

        #region Runtime Resolution Tests

        [Test]
        public void EmittedFrameworkTypeStringsResolveThroughTypeGetTypeTest()
        {
            // The end-to-end invariant: whatever the generator writes, the invocation
            // processor must be able to turn back into a Type.
            var generatedCode = RunGeneratorForInterface("""
                                    Task<byte[]> DownloadAsync(Guid blobId);
                                    Task<int> UploadAsync(byte[] data, string fileName);
                                    Task StoreAsync(byte[][] jagged, int[,] grid);
                                    """);

            var emitted = ExtractParameterTypes(generatedCode)
                .Concat(ExtractAllTaskResultTypes(generatedCode))
                .Where(type => type.StartsWith("System.", StringComparison.Ordinal))
                .ToList();

            Assert.That(emitted, Is.Not.Empty);

            foreach (var type in emitted)
                Assert.That(Type.GetType(type), Is.Not.Null, $"Type.GetType could not resolve '{type}'.");
        }

        #endregion

        #region Tools

        private string RunGeneratorForInterface(string members)
        {
            var source = $$"""
                        {{HEADER}}
                            [ProxyTarget]
                            public interface IBlobService
                            {
                                {{members}}
                            }
                        {{FOOTER}}
                        """;

            var generatedCode = RunGenerator(source, out var diagnostics);

            Assert.That(diagnostics, Is.Empty);

            return generatedCode;
        }

        private static IReadOnlyList<string> ExtractParameterTypes(string generatedCode)
        {
            return Regex.Matches(generatedCode, @"ParametersTypes = new string\[\] \{ (?<body>[^}]*) \}")
                .SelectMany(match => ExtractLiterals(match.Groups["body"].Value))
                .ToList();
        }

        private static string ExtractTaskResultType(string generatedCode)
        {
            return ExtractAllTaskResultTypes(generatedCode).Single();
        }

        /// <summary>
        /// Only invocations that actually carry a task result: a plain <c>Task</c> method
        /// legitimately emits an empty <c>TaskResultType</c>.
        /// </summary>
        private static IReadOnlyList<string> ExtractAllTaskResultTypes(string generatedCode)
        {
            return Regex.Matches(generatedCode, "ReturnsTaskWithResult = (?<flag>true|false),\\s*TaskResultType = \"(?<value>[^\"]*)\"")
                .Where(match => match.Groups["flag"].Value == "true")
                .Select(match => match.Groups["value"].Value)
                .ToList();
        }

        private static IEnumerable<string> ExtractLiterals(string body)
        {
            return Regex.Matches(body, "\"(?<value>[^\"]*)\"")
                .Select(match => match.Groups["value"].Value);
        }

        #endregion
    }
}
