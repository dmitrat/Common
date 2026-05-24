namespace OutWit.Common.Interfaces
{
    /// <summary>
    /// Generic description of "an assembly that lives somewhere on disk."
    /// Used by code that needs to know about an assembly's name, its
    /// physical DLL path, and the directory the assembly conceptually
    /// "belongs to" (its home directory) — independently of whether the
    /// assembly is actually loaded, and independently of any
    /// runtime-reported <see cref="System.Reflection.Assembly.Location"/>.
    /// </summary>
    /// <remarks>
    /// Motivation: there is a class of "assembly is here on disk and
    /// reads files next to itself" scenarios (plugin loaders staging
    /// modules into a known folder, embedded resource bundles,
    /// configuration files sitting next to a component DLL) where
    /// <see cref="System.Reflection.Assembly.Location"/> is the wrong
    /// answer — typically because the loader resolved the assembly from
    /// somewhere other than the runtime's reported load location.
    /// Components that produce such "I know where it lives" knowledge
    /// expose it as <see cref="IAssemblyContext"/>; components that
    /// consume it (e.g. config-file resolvers) take an
    /// <see cref="IAssemblyContext"/> instead of a
    /// <see cref="System.Reflection.Assembly"/> reference.
    /// Producer and consumer stay decoupled through this shared
    /// interface.
    /// </remarks>
    public interface IAssemblyContext
    {
        /// <summary>
        /// A human-readable identifier for the assembly. Typically the
        /// assembly's simple name, or — for plugin scenarios — the
        /// plugin's manifest name.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Absolute path to the assembly's DLL on disk, as the
        /// producer of this context resolved it (e.g. the staged
        /// module path the plugin loader scanned). May differ from
        /// <see cref="System.Reflection.Assembly.Location"/> when the
        /// runtime ended up loading the assembly from a different
        /// path (project-reference graph copies, shared
        /// AssemblyLoadContext lookups, etc.).
        /// </summary>
        string AssemblyPath { get; }

        /// <summary>
        /// Absolute path to the directory that contains
        /// <see cref="AssemblyPath"/> — i.e. the "home" of the
        /// assembly from the producer's perspective. Consumers that
        /// look for files "next to the assembly" (config files,
        /// resource bundles, sibling DLLs) should resolve against
        /// this directory rather than against
        /// <c>Path.GetDirectoryName(Assembly.Location)</c>.
        /// </summary>
        string HomeDirectory { get; }
    }
}
