using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OutWit.Common.Random
{
    public static class RandomUtils
    {
        #region Constants

        private const string DEFAULT_ALLOWED_CHARS = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

        #endregion

        public static string RandomString(ushort length, string allowedChars = DEFAULT_ALLOWED_CHARS)
        {
            if(allowedChars.Length > byte.MaxValue)
                throw new ArgumentException($"allowedChars may contain no more than {byte.MaxValue} characters");

            using (var random = System.Security.Cryptography.RandomNumberGenerator.Create())
            {
                var result = new StringBuilder();
                var bytes = new byte[byte.MaxValue];

                while (result.Length < length)
                {
                    random.GetBytes(bytes);
                    for (int i = 0; i < bytes.Length && result.Length < length; i++)
                    {
                        var index = bytes[i] % allowedChars.Length;
                        result.Append(allowedChars[index]);
                    }

                }

                return result.ToString();
            }
        }

        public static string RandomString()
        {
            var guidString =  Convert.ToBase64String(Guid.NewGuid().ToByteArray()).
                Replace("=", "").
                Replace("+", "").
                Replace("/", "");

            return $"A{guidString}";
        }

        public static string RandomTempFile(string extension = "tmp")
        {
            return RandomTempFile(Path.GetTempPath(), extension);
        }

        public static string RandomTempFile(string tempFolder, string extension)
        {
            var fileName = $"{RandomString()}.{extension}";

            return Path.Combine(tempFolder, fileName);

        }

        public static TEnum NextEnum<TEnum>(this System.Random me)
            where TEnum: Enum
        {
            IReadOnlyList<TEnum> values = Enum.GetValues(typeof(TEnum)).Cast<TEnum>().ToList();
            return values.Count == 0 ? default : values[me.Next(0, values.Count)];
        }
    }
}
