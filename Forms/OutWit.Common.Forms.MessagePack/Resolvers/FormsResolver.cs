using System;
using MessagePack;
using MessagePack.Formatters;
using MessagePack.Resolvers;

namespace OutWit.Common.Forms.MessagePack.Resolvers
{
    /// <summary>
    /// A formatter for the form model and for nothing else.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The form model carries no MessagePack attributes on purpose — it is described once, for
    /// MemoryPack, which is what WitRPC speaks — so a MessagePack-standardised product needs
    /// something that will format it anyway. The obvious answer, registering
    /// <see cref="ContractlessStandardResolver"/> globally, is the wrong one: it makes *every*
    /// unattributed type serialisable, so a model of that product's own that has lost its
    /// <c>[MessagePackObject]</c> stops throwing and quietly starts travelling in a different
    /// shape. Silence is exactly what that product does not need.
    /// </para>
    /// <para>
    /// So the loosening is scoped to one assembly: contractless for the form model, unchanged
    /// behaviour for everything else. Membership is by assembly rather than by namespace, because
    /// a namespace is a string anybody can write and an assembly is the thing that was shipped.
    /// </para>
    /// <para>
    /// Written as a resolver rather than as eight hand-written formatters for the same reason the
    /// model has no hand-written comparisons: a formatter per type is a second description of the
    /// model, maintained by hand, that goes quietly out of step the first time a field is added.
    /// </para>
    /// </remarks>
    public sealed class FormsResolver : IFormatterResolver
    {
        #region Constants

        public static readonly FormsResolver Instance = new();

        /// <summary>The assembly whose types this answers for.</summary>
        private static readonly System.Reflection.Assembly FORMS = typeof(Model.FormSchema).Assembly;

        #endregion

        #region Classes

        private static class Cache<T>
        {
            static Cache()
            {
                Formatter = Belongs(typeof(T))
                    ? ContractlessStandardResolver.Instance.GetFormatter<T>()
                    : null;
            }

            public static IMessagePackFormatter<T>? Formatter { get; }
        }

        #endregion

        #region Constructors

        private FormsResolver()
        {
        }

        #endregion

        #region IFormatterResolver

        /// <summary>
        /// A formatter for a type of the form model, or nothing — which lets the next resolver in
        /// the chain answer, exactly as it would have.
        /// </summary>
        public IMessagePackFormatter<T>? GetFormatter<T>()
        {
            return Cache<T>.Formatter;
        }

        #endregion

        #region Tools

        /// <remarks>
        /// Enumerations of the model come through here too — they are in the same assembly — and a
        /// generic built out of the model's types (a list of fields, a dictionary of values) does
        /// not: it belongs to the framework, and the resolver chain formats it as it always did,
        /// coming back here for each element.
        /// </remarks>
        private static bool Belongs(Type type)
        {
            return type.Assembly == FORMS;
        }

        #endregion
    }
}
