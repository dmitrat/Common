using System;
using System.Collections.Generic;
using System.Linq;
using OutWit.Common.Licensing.Abstract;

namespace OutWit.Common.Licensing.Demo
{
    /// <summary>
    /// Builds the payload that represents the demo period.
    /// <para>
    /// The demo is expressed as an ordinary <see cref="LicensePayload"/> and
    /// judged by the ordinary term rules, so there is exactly one path through
    /// the code. A separate <c>if (noLicence)</c> branch is where licensing bugs
    /// live, and it is also where "delete a file to get the product back" lives.
    /// </para>
    /// <para>
    /// It is never signed and never written to the store — it exists only in
    /// memory, anchored to the first-run stamp in the sidecar state.
    /// </para>
    /// </summary>
    public static class DemoLicenseFactory
    {
        #region Functions

        /// <summary>
        /// Creates the demo payload for a product, anchored at
        /// <paramref name="firstRunUtc"/>.
        /// </summary>
        public static LicensePayload Create(string product, LicenseDemoOptions options, DateTime firstRunUtc)
        {
            return new LicensePayload
            {
                Id = "demo",
                IssuedUtc = firstRunUtc,
                Product = product,
                Edition = LicenseDemoOptions.DEMO_EDITION,
                NotBeforeUtc = firstRunUtc,
                ExpiresUtc = firstRunUtc + options.Term,
                Binding = LicenseBinding.None(),
                Features = options.Features.ToList(),
                Limits = new Dictionary<string, long>(options.Limits, StringComparer.OrdinalIgnoreCase)
            };
        }

        #endregion
    }
}
