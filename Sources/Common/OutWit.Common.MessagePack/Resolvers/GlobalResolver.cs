using System;
using MessagePack;
using MessagePack.Formatters;
using MessagePack.Resolvers;

namespace OutWit.Common.MessagePack.Resolvers
{
    public class GlobalResolver : IFormatterResolver
    {
        #region Classes

        private static class Cache<T>
        {
            public static IMessagePackFormatter<T> Formatter;

            static Cache()
            {

                foreach (var resolver in Resolvers)
                {
                    var f = resolver.GetFormatter<T>();
                    if (f != null)
                    {
                        Formatter = f;
                        return;
                    }
                }
            }
        }

        #endregion

        #region Static Fields

        public static readonly GlobalResolver Instance = new GlobalResolver();

        private static readonly IFormatterResolver[] Resolvers = new IFormatterResolver[]
        {
            SystemTypeResolver.Instance,
            StandardResolver.Instance,
            //AttributeFormatterResolver.Instance,
            //PrimitiveObjectResolver.Instance
        }; 

        #endregion

        #region Constructors

        private GlobalResolver()
        {

        }

        #endregion

        #region Functions

        public IMessagePackFormatter<T> GetFormatter<T>()
        {
            return Cache<T>.Formatter;
        }

        #endregion
    }
}
