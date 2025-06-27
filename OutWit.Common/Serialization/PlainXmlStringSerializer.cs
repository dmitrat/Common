using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using OutWit.Common.Interfaces;

namespace OutWit.Common.Serialization
{
    internal class PlainXmlStringSerializer : ISerializer<string>
    {
        #region Constructors

        public PlainXmlStringSerializer(Type type, Encoding encoding)
        {
            Encoding = encoding ?? Encoding.Unicode;
            Serializer = new XmlSerializer(type);
        }


        public PlainXmlStringSerializer(Type type) 
            : this(type, Encoding.Unicode)
        {
            Serializer = new XmlSerializer(type);
        }

        #endregion

        #region Functions

        public bool Serialize(object obj, out string xmlString)
        {
            xmlString = null;

            using (var stream = new StringWriterEx(Encoding))
            using (var writer = new XmlTextWriter(stream) { Formatting = Formatting.Indented })
            {
                if (!SaveToXml(writer, obj))
                    return false;

                xmlString = stream.GetStringBuilder().ToString();
                return true;
            }
        }

        private bool SaveToXml(XmlWriter writer, object obj)
        {
            try
            {
                using (var wrt = XmlWriter.Create(writer, new XmlWriterSettings {Indent = true, OmitXmlDeclaration = false, Encoding = Encoding}))
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

        public bool Deserialize(string xmlString, out object obj)
        {
            using (var stream = new StringReader(xmlString))
            using (var reader = new XmlTextReader(stream))
                return LoadFromXml(reader, out obj);
        }

        private bool LoadFromXml(XmlReader reader, out object obj)
        {
            obj = null;

            try
            {
                obj = Serializer.Deserialize(reader);
            }
            catch (Exception ex)
            {
                return false;
            }

            return true;
        } 

        #endregion

        #region Properties

        private Encoding Encoding { get; }

        public XmlSerializer Serializer { get; }

        #endregion

    }
}
