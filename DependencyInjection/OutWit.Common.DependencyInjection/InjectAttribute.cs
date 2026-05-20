using System;
using AspectInjector.Broker;

namespace OutWit.Common.DependencyInjection
{
    /// <summary>
    /// Controls how an <see cref="InjectAttribute"/> property is resolved and cached.
    /// </summary>
    public enum InjectMode
    {
        /// <summary>
        /// Resolve once from the owning <see cref="IServiceProvider"/> and cache in the
        /// backing field. Subsequent reads return the cached value.
        /// </summary>
        Cached = 0,

        /// <summary>
        /// Resolve from the owning <see cref="IServiceProvider"/> on every property access.
        /// Useful for resolving fresh scoped/transient services from a singleton owner.
        /// </summary>
        Transient = 1,

        /// <summary>
        /// Create a dedicated child <see cref="Microsoft.Extensions.DependencyInjection.IServiceScope"/>
        /// on first access, resolve the service from it, and reuse the same scope for subsequent
        /// reads. The scope is disposed when the owner's <c>Dispose</c> /
        /// <c>DisposeAsync</c> is invoked.
        /// </summary>
        Scoped = 2
    }

    /// <summary>
    /// Controls whether an <see cref="InjectAttribute"/> property is required (throws if
    /// the service is not registered) or optional (returns <c>null</c>).
    /// </summary>
    public enum InjectRequirement
    {
        /// <summary>
        /// Auto-detect from declared nullability of the property:
        /// non-nullable → required, nullable → optional.
        /// </summary>
        Auto = 0,

        /// <summary>
        /// Always required. Uses <c>GetRequiredService</c> (throws if not registered).
        /// </summary>
        Required = 1,

        /// <summary>
        /// Always optional. Uses <c>GetService</c> (returns <c>null</c> if not registered).
        /// </summary>
        Optional = 2
    }

    /// <summary>
    /// Marks a property for dependency injection from an <see cref="IServiceProvider"/>.
    /// <para>
    /// Default behavior is nullability-aware: non-nullable properties use
    /// <c>GetRequiredService</c>, nullable properties use <c>GetService</c>. Override with
    /// <see cref="Requirement"/>. Override the resolution lifetime with <see cref="Mode"/>.
    /// </para>
    /// <para>
    /// Aliases for common combinations: <see cref="InjectScopedAttribute"/>,
    /// <see cref="InjectTransientAttribute"/>, <see cref="InjectOptionalAttribute"/>,
    /// <see cref="InjectRequiredAttribute"/>.
    /// </para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, Inherited = true)]
    [Injection(typeof(InjectAspect))]
    public class InjectAttribute : Attribute
    {
        /// <summary>
        /// Overrides the nullability-based required/optional detection.
        /// Default: <see cref="InjectRequirement.Auto"/>.
        /// </summary>
        public InjectRequirement Requirement { get; set; } = InjectRequirement.Auto;

        /// <summary>
        /// Selects how this property is resolved and cached.
        /// Default: <see cref="InjectMode.Cached"/>.
        /// </summary>
        public InjectMode Mode { get; set; } = InjectMode.Cached;
    }

    /// <summary>
    /// Shorthand for <c>[Inject(Mode = InjectMode.Scoped)]</c>. Each scoped property
    /// opens its own child <c>IServiceScope</c> on first access; the scope is disposed
    /// with the owner. Canonical use case: injecting a scoped DbContext into a
    /// singleton service.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, Inherited = true)]
    [Injection(typeof(InjectAspect))]
    public sealed class InjectScopedAttribute : InjectAttribute
    {
        /// <summary>
        /// Creates the alias with <see cref="InjectMode.Scoped"/> preset.
        /// </summary>
        public InjectScopedAttribute()
        {
            Mode = InjectMode.Scoped;
        }
    }

    /// <summary>
    /// Shorthand for <c>[Inject(Mode = InjectMode.Transient)]</c>. The property is
    /// resolved from the service provider on every access and never cached.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, Inherited = true)]
    [Injection(typeof(InjectAspect))]
    public sealed class InjectTransientAttribute : InjectAttribute
    {
        /// <summary>
        /// Creates the alias with <see cref="InjectMode.Transient"/> preset.
        /// </summary>
        public InjectTransientAttribute()
        {
            Mode = InjectMode.Transient;
        }
    }

    /// <summary>
    /// Shorthand for <c>[Inject(Requirement = InjectRequirement.Optional)]</c>. Forces
    /// optional resolution regardless of the property's nullability annotation.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, Inherited = true)]
    [Injection(typeof(InjectAspect))]
    public sealed class InjectOptionalAttribute : InjectAttribute
    {
        /// <summary>
        /// Creates the alias with <see cref="InjectRequirement.Optional"/> preset.
        /// </summary>
        public InjectOptionalAttribute()
        {
            Requirement = InjectRequirement.Optional;
        }
    }

    /// <summary>
    /// Shorthand for <c>[Inject(Requirement = InjectRequirement.Required)]</c>. Forces
    /// required resolution regardless of the property's nullability annotation —
    /// throws if the service is not registered.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, Inherited = true)]
    [Injection(typeof(InjectAspect))]
    public sealed class InjectRequiredAttribute : InjectAttribute
    {
        /// <summary>
        /// Creates the alias with <see cref="InjectRequirement.Required"/> preset.
        /// </summary>
        public InjectRequiredAttribute()
        {
            Requirement = InjectRequirement.Required;
        }
    }
}
