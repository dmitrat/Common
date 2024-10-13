using System;
using System.Collections.Generic;
using System.Text;
using MessagePack;
using MessagePack.Formatters;
using OutWit.Common.MessagePack.Formatters;

namespace OutWit.Common.MessagePack.Resolvers
{
    public class SystemTypeResolver : IFormatterResolver
    {
        #region StaticFields

        private static SystemTypeResolver m_instance = null;

        #endregion

        #region Constructors

        private SystemTypeResolver()
        {

        }

        #endregion

        #region Functions

        public IMessagePackFormatter<T> GetFormatter<T>()
        {
            if (typeof(T) == typeof(Type))
                return (IMessagePackFormatter<T>)SystemTypeFormatter.Instance;

            return null;

        } 

        #endregion

        #region Properties

        public static SystemTypeResolver Instance
        {
            get
            {
                if (m_instance == null)
                {
                    m_instance = new SystemTypeResolver();
                }

                return m_instance;
            }
        }

        #endregion
    }
}
