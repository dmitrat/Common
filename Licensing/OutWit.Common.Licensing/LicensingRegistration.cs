using System;
using System.Collections.Generic;

namespace OutWit.Common.Licensing
{
    /// <summary>
    /// The one place every <c>ConfigureLicensing</c> contribution lands, so a
    /// modular host ends up with a single set of options and a single service.
    /// <para>
    /// Held in the service collection rather than in a static field: two
    /// containers in one process — which is what a test suite is — must not
    /// silently share a configuration.
    /// </para>
    /// </summary>
    internal sealed class LicensingRegistration
    {
        #region Properties

        public List<Action<IServiceProvider, LicensingOptions>> Configurators { get; } = new();

        #endregion
    }
}
