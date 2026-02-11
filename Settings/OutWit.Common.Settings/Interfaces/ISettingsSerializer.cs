using System;

namespace OutWit.Common.Settings.Interfaces
{
    public interface ISettingsSerializer
    {
        /// <summary>
        /// Identifier for this serializer, e.g. "String", "Boolean", "Enum".
        /// Used to match entries from storage to the correct serializer.
        /// </summary>
        string ValueKind { get; }

        /// <summary>
        /// The CLR type this serializer handles.
        /// </summary>
        Type ValueType { get; }

        /// <summary>
        /// Deserializes a string value from storage into a typed object.
        /// </summary>
        /// <param name="value">The serialized string value.</param>
        /// <param name="tag">Additional metadata (e.g. enum type name).</param>
        object Deserialize(string value, string tag);

        /// <summary>
        /// Serializes a typed object into a string for storage.
        /// </summary>
        string Serialize(object value);

        /// <summary>
        /// Compares two values for equality.
        /// </summary>
        bool AreEqual(object a, object b);
    }
}
