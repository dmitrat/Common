using System;
using Avalonia.Controls;
using Avalonia.Controls.Templates;

namespace OutWit.Common.MVVM.Avalonia.Utils
{
    /// <summary>
    /// Utility methods for creating DataTemplates in code.
    /// </summary>
    public static class DataTemplateUtils
    {
        /// <summary>
        /// Creates a simple DataTemplate that creates an instance of the specified control type.
        /// </summary>
        public static FuncDataTemplate<TData> Create<TData, TControl>()
            where TControl : Control, new()
        {
            return new FuncDataTemplate<TData>((_, _) => new TControl());
        }

        /// <summary>
        /// Creates a DataTemplate with a custom build function.
        /// </summary>
        public static FuncDataTemplate<TData> Create<TData>(Func<TData, Control> build)
        {
            return new FuncDataTemplate<TData>((data, _) => build(data));
        }
    }
}
