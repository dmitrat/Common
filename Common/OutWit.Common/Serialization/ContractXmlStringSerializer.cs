using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Xml;
using OutWit.Common.Interfaces;

namespace OutWit.Common.Serialization
{
    internal class ContractXmlStringSerializer: ISerializer<DataContractSerializer, string>
    {
        #region Constructors

        public ContractXmlStringSerializer(Type type)
        {
            Serializer = new DataContractSerializer(type);
        }

        public ContractXmlStringSerializer(Type type, IEnumerable<Type> knownTypes)
        {
            Serializer = new DataContractSerializer(type, knownTypes);
        }

        #endregion

        #region Functions

        public bool Serialize(object obj, out string xmlString)
        {
            xmlString = null;

            using (var stream = new StringWriter())
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
                using (var wrt = XmlWriter.Create(writer, new XmlWriterSettings { Indent = true }))
                    Serializer.WriteObject(wrt, obj);
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
                obj = Serializer.ReadObject(reader);
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
