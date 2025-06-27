using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace OutWit.Common.Utils
{
    public static class PathUtils
    {
        public static string AppendPath(this string me, string path)
        {
            return Path.Combine(me, path);
        }

        public static string DirectoryName(this string me)
        {
            return Path.GetDirectoryName(me);
        }

        public static bool IsFullPath(this string me)
        {
            if (string.IsNullOrEmpty(me))
                return false;

#if NET6_0_OR_GREATER

            return Path.IsPathFullyQualified(me);
#else

            return !string.IsNullOrWhiteSpace(me)
                   && me.IndexOfAny(System.IO.Path.GetInvalidPathChars().ToArray()) == -1
                   && Path.IsPathRooted(me)
                   && !Path.GetPathRoot(me).Equals(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal);
#endif

        }

        public static string BuildDataPath(this Assembly me, string basePath, int maxDepth, string subfolder = null)
        {
            var assemblyName = me.GetName().Name;
            if (assemblyName is null)
                throw new InvalidOperationException("Assembly name is null.");

            IEnumerable<string> nameParts = assemblyName.Split('.').Take(maxDepth);

            var pathSegments = new List<string> { basePath };
            pathSegments.AddRange(nameParts);

            if (!string.IsNullOrEmpty(subfolder))
            {
                pathSegments.Add(subfolder);
            }

            return Path.Combine(pathSegments.ToArray());
        }

        public static string ApplicationDataPath(this Assembly me, int maxDepth, string subfolder = null)
        {
            var basePath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return me.BuildDataPath(basePath, maxDepth, subfolder);
        }

        public static string ProgramDataPath(this Assembly me, int maxDepth, string subfolder = null)
        {
            var basePath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            return me.BuildDataPath(basePath, maxDepth, subfolder);
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

        public static bool CheckFolder(this string me)
        {
            if (string.IsNullOrEmpty(me))
                return false;
            
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

        public static bool CopyFile(string sourceFilePath, string destinationFilePath, bool deleteSource)
        {
            try
            {

                File.Copy(sourceFilePath, destinationFilePath);

                if (deleteSource)
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

    }
}
