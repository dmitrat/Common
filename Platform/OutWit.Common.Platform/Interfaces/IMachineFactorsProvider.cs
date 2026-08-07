using System.Collections.Generic;
using System.Threading.Tasks;
using OutWit.Common.Platform.Models.MachineIdentity;

namespace OutWit.Common.Platform.Interfaces
{
    /// <summary>
    /// Collects the individual, independently-observable properties of the
    /// current machine.
    /// <para>
    /// The companion of <see cref="IMachineIdentityProvider"/>, which answers
    /// "what is this machine" with a single hash. This one answers the same
    /// question as a list, so a consumer can decide how much of it has to still
    /// match — the difference between a licence that survives a memory upgrade
    /// and one that does not.
    /// </para>
    /// </summary>
    public interface IMachineFactorsProvider
    {
        /// <summary>
        /// Returns every factor that could be read on this host.
        /// <para>
        /// Factors that cannot be determined are <b>omitted</b>, never returned
        /// empty — so an absent factor is distinguishable from an empty one, and
        /// a consumer counting matches is never fooled by two blanks agreeing.
        /// The collection may therefore be shorter on one host than on another,
        /// and may even be empty.
        /// </para>
        /// </summary>
        Task<IReadOnlyList<MachineFactor>> CollectAsync();
    }
}
