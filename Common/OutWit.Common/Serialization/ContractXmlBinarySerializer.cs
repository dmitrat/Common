using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Xml;
using OutWit.Common.Interfaces;

namespace OutWit.Common.Serialization
{
    internal class ContractXmlBinarySerializer : ISerializer<DataContractSerializer, byte[]>
    {
        #region Constructors

        public ContractXmlBinarySerializer(Type type, IEnumerable<Type> knownTypes)
        {
            Serializer = new DataContractSerializer(type, knownTypes);
        }

        public ContractXmlBinarySerializer(Type type)
        {
            Serializer = new DataContractSerializer(type);
        }

        #endregion

        #region Functions

        public bool Serialize(object obj, out byte[] xmlBytes)
        {
            xmlBytes = null;

            using (var stream = new MemoryStream())
            using (var writer = XmlDictionaryWriter.CreateBinaryWriter(stream))
            {
                if (!Serialize(writer, obj))
                    return false;

                xmlBytes = stream.ToArray();
                return true;
            }
        }

        private bool Serialize(XmlWriter writer, object obj)
        {
            try
            {
                using (var wrt = XmlWriter.Create(writer, new XmlWriterSettings { Indent = true }))
                    Serializer.WriteObject(wrt, obj);
            }
            catch (Exception ex)
            {
                return false;
            }

            return true;
        }

        public bool Deserialize(byte[] xmlBytes, out object value)
        {
            using (var stream = new MemoryStream(xmlBytes))
            using (var reader = XmlDictionaryReader.CreateBinaryReader(stream, XmlDictionaryReaderQuotas.Max))
                return Deserialize(reader, out value);
        }

        private bool Deserialize(XmlDictionaryReader reader, out object value)
        {
            value = null;

            try
            {
                value = Serializer.ReadObject(reader);
            }
            catch (Exception ex)
            {
                return false;
            }

            return true;
        } 

        #endregion

        #region Properties
        public DataContractSerializer Serializer { get; }

        #endregion

    }
}
