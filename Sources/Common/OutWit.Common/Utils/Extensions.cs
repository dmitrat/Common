using System;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Security.Cryptography;
using OutWit.Common.Abstract;
using OutWit.Common.Exceptions;

namespace OutWit.Common.Utils
{
    public static class Extensions
    {
        #region Classes

        public class CommonExtensions { }

        #endregion

        public static string MD5String(this string me)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(me))
                {
                    var hash = md5.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
            }
        }

        public static string ApplicationDataPath(this Assembly me, int maxDepth, string subfolder = null)
        {
            var assemblyName = me.GetName().Name;

            int depth = 0;
            var baseFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            foreach (var part in assemblyName.Split('.'))
            {
                if(depth == maxDepth)
                    break;

                baseFolder = baseFolder.AppendPath(part);
                depth++;
            }

            if (!string.IsNullOrEmpty(subfolder))
                baseFolder = baseFolder.AppendPath(subfolder);

            return baseFolder;
        }

        public static string AssemblyDirectory(this Assembly me)
        {
            return Path.GetDirectoryName(me.Location);
        }

        public static string GetFullPath(this string me)
        {
            return me.GetFullPath(Assembly.GetExecutingAssembly());
        }

        public static string GetFullPath(this string me, Assembly assembly)
        {
            if (string.IsNullOrWhiteSpace(me))
                return "";

            if (me.IsFullPath())
                return me;

            var assemblyPath = assembly.Location;
            var assemblyFolder = Path.GetDirectoryName(assemblyPath);

            if (string.IsNullOrEmpty(assemblyFolder))
                return "";

            var templatesPath = Path.Combine(assemblyFolder, me);
            if (Directory.Exists(templatesPath))
                return Path.GetFullPath(templatesPath);

            return "";
        }

        public static bool IsFullPath(this string me)
        {
            return !string.IsNullOrWhiteSpace(me)
                   && me.IndexOfAny(System.IO.Path.GetInvalidPathChars().ToArray()) == -1
                   && Path.IsPathRooted(me)
                   && !Path.GetPathRoot(me).Equals(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal);
        }

        public static void ThrowDelegateException<TClass>(Delegate fun, Enum status)
            where TClass : class
        {
            var errorMessage = $"Method: {fun.Target}.{fun.Method.Name}; Status: {status}";
            throw new ExceptionOf<TClass>(errorMessage);
        }

        public static bool CheckFolder(this string me)
        {
            if (Directory.Exists(me))
                return true;

            try
            {
                Directory.CreateDirectory(me);
            }
            catch (Exception e)
            {
                return false;
            }

            return true;
        }

        public static string AppendPath(this string me, string path)
        {
            var head = me.TrimEnd('\\');
            var tail = path.TrimStart('\\');

            return $"{head}\\{tail}";


        }

        public static string FileName(this string me)
        {
            if (string.IsNullOrEmpty(me))
                return "";

            try
            {
                return Path.GetFileName(me);
            }
            catch (Exception e)
            {
                return "";
            }
        }

        public static string PathToUrl(this string me, string basePath)
        {
            return me.Replace(basePath, "").Replace("\\", "/");
        }

        public static string UrlToPath(this string me, string basePath)
        {
            if (me[0] != '/')
                basePath = basePath + "\\";

            return basePath + me.Replace("/", "\\");
        }

        public static string DirectoryName(this string me)
        {
            return me.Remove(0, me.LastIndexOf("\\", StringComparison.Ordinal) + 1);
        }

        public static bool CopyFile(string sourceFilePath, string destinationFilePath, bool deleteSource)
        {
            try
            {

                File.Copy(sourceFilePath, destinationFilePath);

                if(deleteSource)
                    File.Delete(sourceFilePath);
            }
            catch (Exception e)
            {
                return false;
            }

            return true;
        }

        public static bool CopyFolder(string sourceFolderPath, string destinationFolderPath, bool deleteSource)
        {
            if (!Directory.Exists(sourceFolderPath) || !destinationFolderPath.CheckFolder())
                return false;

            foreach (var path in Directory.GetFiles(sourceFolderPath))
                CopyFile(path, destinationFolderPath.AppendPath(Path.GetFileName(path)), deleteSource);

            if (!deleteSource)
                return true;

            try
            {
                Directory.Delete(sourceFolderPath);
            }
            catch (Exception e)
            {
                return false;
            }

            return true;

        }

        public static byte ToByte(this bool me)
        {
            return me ? (byte)1 : (byte)0;
        }

        public static string NameOfProperty<T, TResult>(this Expression<Func<T, TResult>> me)
        {
            var member = me.Body as MemberExpression;

            if (member == null || member.Member.MemberType != MemberTypes.Property)
                throw new ExceptionOf<CommonExtensions>($"{me} is not a property");

            return member.Member.Name;
        }

        public static TValue With<TValue>(this TValue me, Action<TValue> setter)
            where TValue:ModelBase
        {
            var clone = (TValue)me.Clone();

            setter(clone);

            return clone;
        }

    }
}
