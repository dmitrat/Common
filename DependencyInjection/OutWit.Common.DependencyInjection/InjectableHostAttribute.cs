using System;

namespace OutWit.Common.DependencyInjection
{
    /// <summary>
    /// Marks a partial class as an inject-host: the
    /// <c>OutWit.Common.DependencyInjection.Generator</c> source generator emits
    /// a constructor accepting <see cref="IServiceProvider"/> and the matching
    /// private <c>Services</c> property required by the
    /// <see cref="InjectAspect"/>'s field auto-discovery.
    /// <para>
    /// Requirements: the target class must be declared <c>partial</c> and must not
    /// declare its own constructor taking a single <see cref="IServiceProvider"/>
    /// (the generator emits one). Classes that need additional constructor
    /// parameters should keep their explicit constructor and skip this attribute.
    /// </para>
    /// <para>
    /// Example:
    /// <code>
    /// [InjectableHost]
    /// public partial class MyService
    /// {
    ///     [Inject] public ILogger&lt;MyService&gt; Logger { get; set; } = null!;
    ///     [Inject] public IRepository? Repository { get; set; }
    /// }
    /// </code>
    /// The generator emits the boilerplate ctor + <c>Services</c> property; the
    /// caller just registers <c>services.AddSingleton&lt;MyService&gt;()</c>.
    /// </para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class InjectableHostAttribute : Attribute
    {
    }
}
