using System.Threading;
using System.Threading.Tasks;
using OutWit.Common.Forms.Model;

namespace OutWit.Common.Forms.Interfaces
{
    /// <summary>
    /// Whoever declared a form, answering what its values mean together.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The schema carries structure — what exists, and what is live while what else is set — and
    /// that is all whoever draws it may decide alone. Everything about meaning is here: which
    /// combinations are acceptable, which limit depends on which other value, what this particular
    /// thing will and will not take.
    /// </para>
    /// <para>
    /// Asynchronous because the authority is frequently not in this process, and given the whole
    /// set of values rather than the one field that changed, because a rule spanning two fields
    /// cannot be answered from either of them alone.
    /// </para>
    /// <para>
    /// A form with no validator is a legitimate arrangement: one whose structure is all the
    /// constraint there is.
    /// </para>
    /// </remarks>
    public interface IFormValidator
    {
        /// <summary>
        /// Says what is wrong with what is currently in the form, or that nothing is.
        /// </summary>
        /// <param name="schema">The form these values belong to.</param>
        /// <param name="values">Everything currently in it.</param>
        /// <param name="cancellation">Cancellation token.</param>
        /// <returns>What is wrong with it, or that nothing is.</returns>
        Task<FormValidation> ValidateAsync(FormSchema schema, FormValues values,
            CancellationToken cancellation = default);
    }
}
