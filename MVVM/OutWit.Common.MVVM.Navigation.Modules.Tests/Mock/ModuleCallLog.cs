using System.Collections.Generic;

namespace OutWit.Common.MVVM.Navigation.Modules.Tests.Mock
{
    /// <summary>
    /// Records what the test modules did, in order.
    /// </summary>
    public sealed class ModuleCallLog
    {
        public List<string> Entries { get; } = new();
    }
}
