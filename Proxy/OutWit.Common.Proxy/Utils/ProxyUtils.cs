using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using OutWit.Common.Proxy.Interfaces;

namespace OutWit.Common.Proxy.Utils
{
    public static class ProxyUtils
    {
        #region Constants

        // Reference-assembly facades a compiler stamps into generated type-name literals
        // ("System.String, System.Runtime, Version=..."). The JIT runtime forwards them to the
        // core library; NativeAOT keeps no facade metadata, so Type.GetType returns null and a
        // typed proxy silently degrades to void/null results. Resolving them to the core
        // library restores the literal's meaning everywhere.
        private static readonly string[] CORE_LIBRARY_FACADES =
        {
            "System.Runtime",
            "System.Private.CoreLib",
            "mscorlib",
            "netstandard",
            "System.Core",
            "System"
        };

        #endregion

        #region Functions

        public static Type[] GetParametersTypes(this IProxyInvocation me)
        {
            if (me.ParametersTypes == null || me.ParametersTypes.Length == 0)
                return Array.Empty<Type>();

            return me.ParametersTypes.Select(ResolveType).ToArray()!;
        }

        public static Type[] GetGenericArguments(this IProxyInvocation me)
        {
            if (me.GenericArguments == null || me.GenericArguments.Length == 0)
                return Array.Empty<Type>();

            return me.GenericArguments.Select(ResolveType).ToArray()!;
        }

        public static Type GetReturnType(this IProxyInvocation me)
        {
            if(string.IsNullOrEmpty(me.ReturnType))
                return typeof(void);

            try
            {
                return ResolveType(me.ReturnType) ?? typeof(void);
            }
            catch (Exception e)
            {
                return typeof(void);
            }
        }

        public static string TypeString(this Type me)
        {
            return $"{me.AssemblyQualifiedName}";
        }

        /// <summary>
        /// Resolves a generated type-name literal (assembly-qualified, possibly with nested
        /// generic arguments) the way <c>Type.GetType</c> does, with core-library facade
        /// names redirected to the runtime's core library so the literal resolves on
        /// NativeAOT as well as on the JIT runtime.
        /// </summary>
        public static Type? ResolveType(string? typeName)
        {
            if (string.IsNullOrEmpty(typeName))
                return null;

            var direct = Type.GetType(typeName, throwOnError: false);
            if (direct != null)
                return direct;

            try
            {
                return Type.GetType(typeName, ResolveAssembly, typeResolver: null, throwOnError: false);
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static Assembly? ResolveAssembly(AssemblyName assemblyName)
        {
            if (assemblyName.Name != null && CORE_LIBRARY_FACADES.Contains(assemblyName.Name, StringComparer.OrdinalIgnoreCase))
                return typeof(object).Assembly;

            try
            {
                return Assembly.Load(assemblyName);
            }
            catch (Exception)
            {
                return null;
            }
        }

        #endregion
    }
}
