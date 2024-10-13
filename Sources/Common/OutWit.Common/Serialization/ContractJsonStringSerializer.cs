using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;
using OutWit.Common.Interfaces;

namespace OutWit.Common.Serialization
{
    internal class ContractJsonStringSerializer : ISerializer<DataContractJsonSerializer, string>
    {
        #region Constructors

        public ContractJsonStringSerializer(Type type)
        {
            Serializer = new DataContractJsonSerializer(type);
        }

        public ContractJsonStringSerializer(Type type, IEnumerable<Type> knownTypes)
        {
            Serializer = new DataContractJsonSerializer(type, knownTypes);
        }

        #endregion

        #region Functions

        public bool Deserialize(string jsonStr, out object obj)
        {
            obj = null;

            try
            {
                using (var stream = new MemoryStream(Encoding.Unicode.GetBytes(jsonStr)))
                    obj = Serializer.ReadObject(stream);

            }
            catch (Exception e)
            {
                return false;
            }

            return true;
        }

        public bool Serialize(object obj, out string jsonStr)
        {
            jsonStr = "";

            try
            {
                using (var stream = new MemoryStream())
                {
                    Serializer.WriteObject(stream, obj);
                    stream.Position = 0;

                    using (var reader = new StreamReader(stream))
                        jsonStr = reader.ReadToEnd();

                }
            }
            catch (Exception ex)
            {
                return false;
            }

            return true;
        } 

        #endregion

        #region Properties

        public DataContractJsonSerializer Serializer { get; }

        #endregion

    }
}
