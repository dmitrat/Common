using System;
using System.Collections.Generic;

namespace OutWit.Common.Fat.Directories
{
    /// <summary>
    /// Paths on a volume: '/' or '\' between components, rooted whether or not they
    /// start with a separator.
    /// </summary>
    internal static class FatPath
    {
        #region Constants

        public const string ROOT = "/";

        private const char SEPARATOR = '/';

        private static readonly char[] SEPARATORS = { '/', '\\' };

        #endregion

        #region Functions

        /// <summary>
        /// Splits a path into its components. Empty components and <c>.</c> are dropped
        /// and <c>..</c> removes the component before it.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">The path climbs above the root.</exception>
        public static IReadOnlyList<string> Split(string path)
        {
            ArgumentNullException.ThrowIfNull(path);

            var components = new List<string>();
            foreach (string part in path.Split(SEPARATORS, StringSplitOptions.RemoveEmptyEntries))
            {
                if (part == ".")
                    continue;

                if (part == "..")
                {
                    if (components.Count == 0)
                        throw new ArgumentException($"The path '{path}' climbs above the root.", nameof(path));
                    components.RemoveAt(components.Count - 1);
                    continue;
                }

                components.Add(part);
            }

            return components;
        }

        /// <summary>
        /// The path of an entry in a directory.
        /// </summary>
        public static string Combine(string directory, string name)
        {
            return directory == ROOT ? ROOT + name : directory + SEPARATOR + name;
        }

        /// <summary>
        /// Joins components into a rooted path.
        /// </summary>
        public static string Join(IEnumerable<string> components)
        {
            return SEPARATOR + string.Join(SEPARATOR, components);
        }

        #endregion
    }
}
