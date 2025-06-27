using System.Runtime.Serialization;

namespace OutWit.Common.Interfaces
{

    internal interface ISerializer<out TInnerSerializer, TSerialize> : ISerializer<TSerialize>
        where TInnerSerializer : XmlObjectSerializer
    {
        TInnerSerializer Serializer { get; }
    }

    internal interface ISerializer<TSerialize>

    {
        bool Serialize(object obj, out TSerialize serializedValue);
        bool Deserialize(TSerialize serializedValue, out object dataObject);
    }
}
