using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using MessagePack;
using MessagePack.Resolvers;
using OutWit.Common.MessagePack.Formatters;
using OutWit.Common.MessagePack.Resolvers;
using OutWit.Common.Random;

namespace OutWit.Common.MessagePack
{
    public static class MessagePackUtils
    {
        static MessagePackUtils()
        {
            //StaticCompositeResolver.Instance.Register(SystemTypeFormatter.Instance);
            //StaticCompositeResolver.Instance.Register(BuiltinResolver.Instance);
            //StaticCompositeResolver.Instance.Register(AttributeFormatterResolver.Instance);
            //StaticCompositeResolver.Instance.Register(PrimitiveObjectResolver.Instance);
            CompressionOption = MessagePackSerializerOptions.Standard.WithCompression(MessagePackCompression.Lz4Block).WithResolver(GlobalResolver.Instance);
            
        }
        
        public static byte[] ToPackBytes<TObject>(this TObject me, bool withCompression = true)
            where TObject : class
        {
            try
            {
                using (var stream = new MemoryStream())
                {
                    if (withCompression)
                        MessagePackSerializer.Serialize(stream, me, CompressionOption);
                    else
                        MessagePackSerializer.Serialize(stream, me);

                    return stream.ToArray();
                }
            }
            catch (Exception e)
            {
                return null;
            }

        }

        public static TObject FromPackBytes<TObject>(this byte[] me, bool withCompression = true)
            where TObject : class
        {
            try
            {
                if (withCompression)
                    return MessagePackSerializer.Deserialize<TObject>(me, CompressionOption);
                else
                    return MessagePackSerializer.Deserialize<TObject>(me);

            }
            catch (Exception e)
            {
                return default(TObject);
            }
        }

        public static object FromPackBytes(this byte[] me, Type type, bool withCompression = true)
        {
            try
            {
                if (withCompression)
                    return MessagePackSerializer.Deserialize(type, me, CompressionOption);
                else
                    return MessagePackSerializer.Deserialize(type, me);

            }
            catch (Exception e)
            {
                return null;
            }
        }

        public static MemoryMappedFile ToPackMemoryMappedFile<TObject>(this TObject me, out string mapName,
            out int length)
            where TObject : class
        {
            var bytes = me.ToPackBytes();

            mapName = RandomUtils.RandomString();
            length = bytes.Length;

            var file = MemoryMappedFile.CreateNew(mapName, length, MemoryMappedFileAccess.ReadWrite);
            using (var stream = file.CreateViewStream(0, 0, MemoryMappedFileAccess.Write))
            {
                stream.Write(bytes, 0, length);
            }

            return file;
        }
        
        public static object FromPackMemoryMappedFile(this string me, int size, Type type)
        {
            using (var file = MemoryMappedFile.OpenExisting(me, MemoryMappedFileRights.Read))
            {
                using (var stream = file.CreateViewStream(0, 0, MemoryMappedFileAccess.Read))
                {
                    var bytes = new byte[size];

                    stream.Read(bytes, 0, size);

                    return bytes.FromPackBytes(type);
                }
            }
        }

        public static TObject FromPackMemoryMappedFile<TObject>(this string me, int size)
            where TObject : class
        {
            using (var file = MemoryMappedFile.OpenExisting(me, MemoryMappedFileRights.Read))
            {
                using (var stream = file.CreateViewStream(0, 0, MemoryMappedFileAccess.Read))
                {
                    var bytes = new byte[size];

                    stream.Read(bytes, 0, size);

                    return bytes.FromPackBytes<TObject>();
                }
            }
        }


        private static MessagePackSerializerOptions CompressionOption { get; }

    }
}
