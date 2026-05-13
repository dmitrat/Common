#if NETSTANDARD2_0

// ReSharper disable once CheckNamespace
namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// Polyfill for the C# 9.0 init-only properties marker on .NET Standard 2.0.
    /// </summary>
    internal static class IsExternalInit
    {
    }
}

#endif
