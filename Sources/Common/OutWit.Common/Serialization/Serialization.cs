using System;
using System.IO.MemoryMappedFiles;
using System.Security.Cryptography;
using System.Text;
using OutWit.Common.Interfaces;
using OutWit.Common.Random;

namespace OutWit.Common.Serialization
{
    public static class Serialization
    {
        #region Common

        private static TSerialize Serialize<TData, TSerialize>(ISerializer<TSerialize> serializer, TData data)
            where TData : class

        {
            return serializer.Serialize(data, out var value) ? value : default(TSerialize);
        }

        private static TData Deserialize<TData, TSerialize>(ISerializer<TSerialize> serializer, TSerialize data)
            where TData : class

        {
            return serializer.Deserialize(data, out var obj) ? (TData)obj : null;
        } 

        #endregion

        #region Json

        public static string ToContractJsonString<TObject>(this TObject me)
            where TObject : class
        {
            return Serialize(new ContractJsonStringSerializer(me.GetType()),  me);
        }

        public static TObject FromContractJsonString<TObject>(this string me)
            where TObject : class
        {
            return Deserialize<TObject, string>(new ContractJsonStringSerializer(typeof(TObject)), me);
        }

        public static object FromContractJsonString(this string me, Type type)
        {
            return Deserialize<object, string>(new ContractJsonStringSerializer(type), me);
        }

        #endregion

        #region Xml

        public static string ToContractXmlString<TObject>(this TObject me)
            where TObject : class
        {
            return Serialize(new ContractXmlStringSerializer(me.GetType()), me);
        }

        public static TObject FromContractXmlString<TObject>(this string me)
            where TObject : class
        {
            return Deserialize<TObject, string>(new ContractXmlStringSerializer(typeof(TObject)), me);
        }

        public static object FromContractXmlString(this string me, Type type)
        {
            return Deserialize<object, string>(new ContractXmlStringSerializer(type), me);
        }

        public static byte[] ToXmlBytes<TObject>(this TObject me, params Type[] knowTypes)
            where TObject : class
        {
            return Serialize(new ContractXmlBinarySerializer(me.GetType(), knowTypes), me);
        }

        public static TObject FromXmlBytes<TObject>(this byte[] me, params Type[] knowTypes)
            where TObject : class
        {
            return Deserialize<TObject, byte[]>(new ContractXmlBinarySerializer(typeof(TObject), knowTypes), me);
        }

        public static object FromXmlBytes(this byte[] me, Type type, params Type[] knowTypes)
        {
            return Deserialize<object, byte[]>(new ContractXmlBinarySerializer(type, knowTypes), me);
        }

        #endregion

        #region PlainXml

        public static string ToPlainXmlString<TObject>(this TObject me, Encoding encoding = null)
            where TObject : class
        {
            return Serialize(new PlainXmlStringSerializer(me.GetType(), encoding), me);
        }

        public static TObject FromPlainXmlString<TObject>(this string me)
            where TObject : class
        {
            return Deserialize<TObject, string>(new PlainXmlStringSerializer(typeof(TObject)), me);
        }

        public static object FromPlainXmlString(this string me, Type type)
        {
            return Deserialize<object, string>(new PlainXmlStringSerializer(type), me);
        }

        public static byte[] ToPlainXmlBytes<TObject>(this TObject me)
            where TObject : class
        {
            return Serialize(new PlainXmlBinarySerializer(me.GetType()), me);
        }

        public static TObject FromPlainXmlBytes<TObject>(this byte[] me)
            where TObject : class
        {
            return Deserialize<TObject, byte[]>(new PlainXmlBinarySerializer(typeof(TObject)), me);
        }

        public static object FromPlainXmlBytes(this byte[] me, Type type)
        {
            return Deserialize<object, byte[]>(new PlainXmlBinarySerializer(type), me);
        }

        #endregion

        #region File

        public static bool ToFile(this string me, string filePath)
        {
            try
            {
                System.IO.File.WriteAllText(filePath, me);
            }
            catch (Exception e)
            {
                return false;
            }

            return true;

        }

        public static string ReadTextFile(string filePath)
        {
            try
            {
                return System.IO.File.ReadAllText(filePath);
            }
            catch (Exception e)
            {
                return null;
            }

        }


        public static bool ToFile(this byte[] me, string filePath)
        {
            try
            {
                System.IO.File.WriteAllBytes(filePath, me);
            }
            catch (Exception e)
            {
                return false;
            }

            return true;

        }

        public static byte[] ReadBinaryFile(string filePath)
        {
            try
            {
                return System.IO.File.ReadAllBytes(filePath);
            }
            catch (Exception e)
            {
                return null;
            }

        }

        public static MemoryMappedFile ToMemoryMappedFile(this byte[] me, out string mapName, out int length)
        {
            mapName = RandomUtils.RandomString();
            length = me.Length;

            var file = MemoryMappedFile.CreateNew(mapName, me.Length, MemoryMappedFileAccess.ReadWrite);
            using (var stream = file.CreateViewStream(0, 0, MemoryMappedFileAccess.Write))
            {
                stream.Write(me, 0, me.Length);
            }

            return file;
        }

        public static byte[] FromMemoryMappedFile(this string me, int size)
        {
            using (var file = MemoryMappedFile.OpenExisting(me, MemoryMappedFileRights.Read))
            {
                using (var stream = file.CreateViewStream(0, 0, MemoryMappedFileAccess.Read))
                {
                    var bytes = new byte[size];

                    stream.Read(bytes, 0, size);

                    return bytes;
                }
            }
        }


        public static MemoryMappedFile ToXmlMemoryMappedFile<TObject>(this TObject me, out string mapName, out int length)
            where TObject : class
        {
            var bytes = me.ToXmlBytes();

            mapName = RandomUtils.RandomString();
            length = bytes.Length;

            var file = MemoryMappedFile.CreateNew(mapName, length, MemoryMappedFileAccess.ReadWrite);
            using (var stream = file.CreateViewStream(0, 0, MemoryMappedFileAccess.Write))
            {
                stream.Write(bytes, 0, length);
            }

            return file;
        }

        public static object FromXmlMemoryMappedFile(this string me, int size, Type type)
        {
            using (var file = MemoryMappedFile.OpenExisting(me, MemoryMappedFileRights.Read))
            {
                using (var stream = file.CreateViewStream(0, 0, MemoryMappedFileAccess.Read))
                {
                    var bytes = new byte[size];

                    stream.Read(bytes, 0, size);

                    return bytes.FromXmlBytes(type);
                }
            }
        }

        public static TObject FromXmlMemoryMappedFile<TObject>(this string me, int size)
            where TObject: class
        {
            using (var file = MemoryMappedFile.OpenExisting(me, MemoryMappedFileRights.Read))
            {
                using (var stream = file.CreateViewStream(0, 0, MemoryMappedFileAccess.Read))
                {
                    var bytes = new byte[size];

                    stream.Read(bytes, 0, size);

                    return bytes.FromXmlBytes<TObject>();
                }
            }
        }

        #endregion
    }
}
