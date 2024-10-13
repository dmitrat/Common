using System;
using MessagePack;
using MessagePack.Formatters;

namespace OutWit.Common.MessagePack.Formatters
{
    public class SystemTypeFormatter : IMessagePackFormatter<Type>
    {
        #region StaticFields

        private static SystemTypeFormatter m_instance = null;

        #endregion

        #region Constructors

        private SystemTypeFormatter()
        {

        }

        #endregion

        #region Functions

        public void Serialize(ref MessagePackWriter writer, Type value, MessagePackSerializerOptions options)
        {
            writer.Write($"{value.FullName}, {value.Assembly.FullName}");
        }

        public Type Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            return Type.GetType(reader.ReadString());
        }

        #endregion

        #region Properties

        public static SystemTypeFormatter Instance
        {
            get
            {
                if (m_instance == null)
                {
                    m_instance = new SystemTypeFormatter();
                }

                return m_instance;
            }
        }

        #endregion
    }
}
