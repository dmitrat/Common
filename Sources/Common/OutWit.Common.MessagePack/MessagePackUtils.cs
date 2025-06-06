using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using MessagePack;
using MessagePack.Resolvers;
using Microsoft.Extensions.Logging;
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
        
        public static byte[] ToPackBytes<TObject>(this TObject me, bool withCompression = true, ILogger logger = null)
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
                logger?.LogError(e, "Failed to serialize to MessagePack");
                return null;
            }

        }

        public static TObject FromPackBytes<TObject>(this byte[] me, bool withCompression = true, ILogger logger = null)
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
                logger?.LogError(e, "Failed to deserialize from MessagePack");
                return default(TObject);
            }
        }

        public static object FromPackBytes(this byte[] me, Type type, bool withCompression = true, ILogger logger = null)
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
                logger?.LogError(e, "Failed to deserialize from MessagePack");
                return type.IsValueType
                    ? Activator.CreateInstance(type)
                    : null;
            }
        }

        public static MemoryMappedFile ToPackMemoryMappedFile<TObject>(this TObject me, out string mapName, out int length, ILogger logger = null)
        {
            var bytes = me.ToPackBytes(logger: logger);

            mapName = RandomUtils.RandomString();
            length = bytes.Length;

            var file = MemoryMappedFile.CreateNew(mapName, length, MemoryMappedFileAccess.ReadWrite);
            using (var stream = file.CreateViewStream(0, 0, MemoryMappedFileAccess.Write))
            {
                stream.Write(bytes, 0, length);
            }

            return file;
        }
        
        public static object FromPackMemoryMappedFile(this string me, int size, Type type, ILogger logger = null)
        {
            using (var file = MemoryMappedFile.OpenExisting(me, MemoryMappedFileRights.Read))
            {
                using (var stream = file.CreateViewStream(0, 0, MemoryMappedFileAccess.Read))
                {
                    var bytes = new byte[size];

                    stream.Read(bytes, 0, size);

                    return bytes.FromPackBytes(type, logger:logger);
                }
            }
        }

        public static TObject FromPackMemoryMappedFile<TObject>(this string me, int size, ILogger logger = null)
            where TObject : class
        {
            using (var file = MemoryMappedFile.OpenExisting(me, MemoryMappedFileRights.Read))
            {
                using (var stream = file.CreateViewStream(0, 0, MemoryMappedFileAccess.Read))
                {
                    var bytes = new byte[size];

                    stream.Read(bytes, 0, size);

                    return bytes.FromPackBytes<TObject>(logger: logger);
                }
            }
        }

        public static TObject PackClone<TObject>(this TObject me, ILogger logger = null)
        {
            var bytes = me.ToPackBytes(logger: logger);
            return bytes == null
                ? default(TObject)
                : bytes.FromPackBytes<TObject>(logger: logger);
        }


        private static MessagePackSerializerOptions CompressionOption { get; }

    }
}
