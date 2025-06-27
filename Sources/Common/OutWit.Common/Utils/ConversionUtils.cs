using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Text;
using OutWit.Common.Serialization;
using OutWit.Common.Values;

namespace OutWit.Common.Utils
{
    public static class ConversionUtils
    {
        #region Bool

        public static byte[] ToBytes(this bool me)
        {
            return BitConverter.GetBytes(me);
        } 

        public static byte[] ToBytes(this IEnumerable<bool> me)
        {
            return me.SelectMany(BitConverter.GetBytes).ToArray();
        }

        public static bool[] ToBoolean(this byte[] me)
        {
            return me.ToValues(sizeof(bool), x => BitConverter.ToBoolean(x, 0));
        }

        public static MemoryMappedFile ToMemoryMappedFile(this IEnumerable<bool> me, out string mapName, out int length)
        {
            return me.ToBytes().ToMemoryMappedFile(out mapName, out length);
        }

        public static bool[] FromMemoryMappedFileAsBoolean(this string me, int size)
        {
            return me.FromMemoryMappedFile(size).ToBoolean();
        }

        #endregion

        #region Char

        public static byte[] ToBytes(this char me)
        {
            return BitConverter.GetBytes(me);
        } 

        public static byte[] ToBytes(this IEnumerable<char> me)
        {
            return me.SelectMany(BitConverter.GetBytes).ToArray();
        } 

        public static char[] ToChar(this byte[] me)
        {
            return me.ToValues(sizeof(char), x => BitConverter.ToChar(x, 0));
        }

        public static MemoryMappedFile ToMemoryMappedFile(this IEnumerable<char> me, out string mapName, out int length)
        {
            return me.ToBytes().ToMemoryMappedFile(out mapName, out length);
        }

        public static char[] FromMemoryMappedFileAsChar(this string me, int size)
        {
            return me.FromMemoryMappedFile(size).ToChar();
        }

        #endregion

        #region Short

        public static byte[] ToBytes(this short me)
        {
            return BitConverter.GetBytes(me);
        } 

        public static byte[] ToBytes(this IEnumerable<short> me)
        {
            return me.SelectMany(BitConverter.GetBytes).ToArray();
        } 

        public static short[] ToShort(this byte[] me)
        {
            return me.ToValues(sizeof(short), x => BitConverter.ToInt16(x, 0));
        }

        public static MemoryMappedFile ToMemoryMappedFile(this IEnumerable<short> me, out string mapName, out int length)
        {
            return me.ToBytes().ToMemoryMappedFile(out mapName, out length);
        }

        public static short[] FromMemoryMappedFileAsShort(this string me, int size)
        {
            return me.FromMemoryMappedFile(size).ToShort();
        }

        #endregion

        #region UShort

        public static byte[] ToBytes(this ushort me)
        {
            return BitConverter.GetBytes(me);
        } 

        public static byte[] ToBytes(this IEnumerable<ushort> me)
        {
            return me.SelectMany(BitConverter.GetBytes).ToArray();
        } 

        public static ushort[] ToUShort(this byte[] me)
        {
            return me.ToValues(sizeof(ushort), x => BitConverter.ToUInt16(x, 0));
        }

        public static MemoryMappedFile ToMemoryMappedFile(this IEnumerable<ushort> me, out string mapName, out int length)
        {
            return me.ToBytes().ToMemoryMappedFile(out mapName, out length);
        }

        public static ushort[] FromMemoryMappedFileAsUShort(this string me, int size)
        {
            return me.FromMemoryMappedFile(size).ToUShort();
        }

        #endregion

        #region Int

        public static byte[] ToBytes(this int me)
        {
            return BitConverter.GetBytes(me);
        } 

        public static byte[] ToBytes(this IEnumerable<int> me)
        {
            return me.SelectMany(BitConverter.GetBytes).ToArray();
        } 

        public static int[] ToInt(this byte[] me)
        {
            return me.ToValues(sizeof(int), x => BitConverter.ToInt32(x, 0));
        }

        public static MemoryMappedFile ToMemoryMappedFile(this IEnumerable<int> me, out string mapName, out int length)
        {
            return me.ToBytes().ToMemoryMappedFile(out mapName, out length);
        }

        public static int[] FromMemoryMappedFileAsInt(this string me, int size)
        {
            return me.FromMemoryMappedFile(size).ToInt();
        }

        #endregion

        #region UInt

        public static byte[] ToBytes(this uint me)
        {
            return BitConverter.GetBytes(me);
        } 

        public static byte[] ToBytes(this IEnumerable<uint> me)
        {
            return me.SelectMany(BitConverter.GetBytes).ToArray();
        } 

        public static uint[] ToUInt(this byte[] me)
        {
            return me.ToValues(sizeof(uint), x => BitConverter.ToUInt32(x, 0));
        }

        public static MemoryMappedFile ToMemoryMappedFile(this IEnumerable<uint> me, out string mapName, out int length)
        {
            return me.ToBytes().ToMemoryMappedFile(out mapName, out length);
        }

        public static uint[] FromMemoryMappedFileAsUInt(this string me, int size)
        {
            return me.FromMemoryMappedFile(size).ToUInt();
        }

        #endregion

        #region Long

        public static byte[] ToBytes(this long me)
        {
            return BitConverter.GetBytes(me);
        } 

        public static byte[] ToBytes(this IEnumerable<long> me)
        {
            return me.SelectMany(BitConverter.GetBytes).ToArray();
        } 

        public static long[] ToLong(this byte[] me)
        {
            return me.ToValues(sizeof(long), x => BitConverter.ToInt64(x, 0));
        }

        public static MemoryMappedFile ToMemoryMappedFile(this IEnumerable<long> me, out string mapName, out int length)
        {
            return me.ToBytes().ToMemoryMappedFile(out mapName, out length);
        }

        public static long[] FromMemoryMappedFileAsLong(this string me, int size)
        {
            return me.FromMemoryMappedFile(size).ToLong();
        }

        #endregion

        #region ULong

        public static byte[] ToBytes(this ulong me)
        {
            return BitConverter.GetBytes(me);
        } 

        public static byte[] ToBytes(this IEnumerable<ulong> me)
        {
            return me.SelectMany(BitConverter.GetBytes).ToArray();
        } 

        public static ulong[] ToULong(this byte[] me)
        {
            return me.ToValues(sizeof(ulong), x => BitConverter.ToUInt64(x, 0));
        }

        public static MemoryMappedFile ToMemoryMappedFile(this IEnumerable<ulong> me, out string mapName, out int length)
        {
            return me.ToBytes().ToMemoryMappedFile(out mapName, out length);
        }

        public static ulong[] FromMemoryMappedFileAsULong(this string me, int size)
        {
            return me.FromMemoryMappedFile(size).ToULong();
        }

        #endregion

        #region Float

        public static byte[] ToBytes(this float me)
        {
            return BitConverter.GetBytes(me);
        } 

        public static byte[] ToBytes(this IEnumerable<float> me)
        {
            return me.SelectMany(BitConverter.GetBytes).ToArray();
        } 

        public static float[] ToFloat(this byte[] me)
        {
            return me.ToValues(sizeof(float), x => BitConverter.ToSingle(x, 0));
        }

        public static MemoryMappedFile ToMemoryMappedFile(this IEnumerable<float> me, out string mapName, out int length)
        {
            return me.ToBytes().ToMemoryMappedFile(out mapName, out length);
        }

        public static float[] FromMemoryMappedFileAsFloat(this string me, int size)
        {
            return me.FromMemoryMappedFile(size).ToFloat();
        }

        #endregion

        #region Double

        public static byte[] ToBytes(this double me)
        {
            return BitConverter.GetBytes(me);
        } 

        public static byte[] ToBytes(this IEnumerable<double> me)
        {
            return me.SelectMany(BitConverter.GetBytes).ToArray();
        } 

        public static double[] ToDouble(this byte[] me)
        {
            return me.ToValues(sizeof(double), x => BitConverter.ToDouble(x, 0));
        }

        public static MemoryMappedFile ToMemoryMappedFile(this IEnumerable<double> me, out string mapName, out int length)
        {
            return me.ToBytes().ToMemoryMappedFile(out mapName, out length);
        }

        public static double[] FromMemoryMappedFileAsDouble(this string me, int size)
        {
            return me.FromMemoryMappedFile(size).ToDouble();
        }

        #endregion

        #region Tools

        private static TValue[] ToValues<TValue>(this byte[] me, int singleSize, Func<byte[], TValue> converter)
        {
            var buffer = new byte[singleSize];
            var values = new List<TValue>(me.Length / singleSize);

            using (var stream = new MemoryStream(me))
            {
                while (stream.Read(buffer, 0, buffer.Length) == buffer.Length)
                {
                    values.Add(converter(buffer));
                }
            }

            return values.ToArray();
        } 

        #endregion

    }
}
