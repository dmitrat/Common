using System.Collections.Generic;
using System.Threading.Tasks;
using OutWit.Common.Licensing.Abstract;

namespace OutWit.Common.Licensing.Binding
{
    /// <summary>
    /// Produces the binding factors for the host the product is running on,
    /// already hashed and ready either to match against a licence or to travel
    /// in an issuance request.
    /// <para>
    /// Nothing here takes a user, a token or a principal. Identity — who you are
    /// and what you may reach — is a separate axis; a licence answers only what
    /// may run on this machine, and the two must never gate each other. Keeping
    /// them apart in the API is what stops a later change from quietly merging
    /// them.
    /// </para>
    /// </summary>
    public interface ILicenseBindingProvider
    {
        /// <summary>The family of factors this provider contributes.</summary>
        LicenseBindingKind Kind { get; }

        /// <summary>
        /// Collects the current factors. Factors that cannot be read are
        /// omitted, never returned blank.
        /// </summary>
        Task<IReadOnlyList<LicenseFactor>> CollectAsync();
    }
}
