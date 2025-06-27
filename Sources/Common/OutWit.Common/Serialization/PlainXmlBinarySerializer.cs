using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Xml;
using System.Xml.Serialization;
using OutWit.Common.Interfaces;

namespace OutWit.Common.Serialization
{
    internal class PlainXmlBinarySerializer: ISerializer<byte[]>
    {
        #region Constructors

        public PlainXmlBinarySerializer(Type type, IEnumerable<Type> knownTypes)
        {
            Serializer = new XmlSerializer(type, knownTypes.ToArray());
        }

        public PlainXmlBinarySerializer(Type type)
        {
            Serializer = new XmlSerializer(type);
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
                using (var wrt = XmlWriter.Create(writer, new XmlWriterSettings {Indent = true}))
                {
                    var xns = new XmlSerializerNamespaces();
                    xns.Add(string.Empty, string.Empty);
                    Serializer.Serialize(wrt, obj, xns);
                }
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
            using (var reader = XmlReader.Create(stream))
                return Deserialize(reader, out value);
        }

        private bool Deserialize(XmlReader reader, out object value)
        {
            value = null;

            try
            {
                value = this.Serializer.Deserialize(reader);
            }
            catch (Exception ex)
            {
                return false;
            }

            return true;
        } 

        #endregion

        #region Properties
        public XmlSerializer Serializer { get; }

        #endregion

    }
}
