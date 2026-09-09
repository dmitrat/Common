using OutWit.Common.Forms.MessagePack.Resolvers;

namespace OutWit.Common.Forms.MessagePack
{
    /// <summary>
    /// Teaching a MessagePack-standardised product to carry a form.
    /// </summary>
    /// <remarks>
    /// One call, once, at startup. After it, <c>FormSchema</c>, <c>FormValues</c> and everything
    /// they are made of travel on that product's own MessagePack wire — through
    /// <c>ToMessagePackBytes</c>, <c>MessagePackClone</c>, or whatever else it already uses — and
    /// nothing else about how it serialises has changed.
    /// </remarks>
    /// <example>
    /// <code>
    /// FormsMessagePack.Register();
    /// </code>
    /// </example>
    public static class FormsMessagePack
    {
        #region Functions

        /// <summary>
        /// Registers the form model with <c>OutWit.Common.MessagePack</c>.
        /// </summary>
        /// <remarks>
        /// Safe to call more than once: the underlying registry keeps resolvers in a bag and asks
        /// them in turn, and this one answers only for its own assembly, so a second copy of it
        /// changes nothing.
        /// </remarks>
        public static void Register()
        {
            // Spelled out from the root: inside this namespace, `OutWit.Common.MessagePack`
            // would be read as a child of `OutWit.Common.Forms`, which does not exist.
            global::OutWit.Common.MessagePack.MessagePackUtils.Register(FormsResolver.Instance);
        }

        #endregion
    }
}
