using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace OutWit.Common.Fat.Directories
{
    /// <summary>
    /// Checks names for new entries and makes their 8.3 aliases.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A name that already is a valid 8.3 name, with its base and its extension each in
    /// one case, is stored as a short entry alone, the case kept in the flags Windows NT
    /// introduced. Any other name gets a long name and an alias.
    /// </para>
    /// <para>
    /// The alias follows the Microsoft specification as Linux and mtools apply it: upper
    /// case; spaces and periods dropped from the base, the extension taken after the last
    /// period; characters an 8.3 name cannot hold, and anything outside ASCII, turned into
    /// '_'; and a numeric tail <c>~n</c>, the smallest free one, whenever the name had to be
    /// changed or the plain alias is taken.
    /// </para>
    /// <para>
    /// Aliases stay in ASCII on purpose. mtools puts letters of code page 437 in them, but a
    /// reader with another OEM code page shows those bytes as other letters; and mtools
    /// adds no tail when it only replaced characters with '_', which the specification
    /// counts as a lossy change.
    /// </para>
    /// </remarks>
    internal static class ShortNameBuilder
    {
        #region Constants

        private const string SPECIAL = "!#$%&'()-@^_`{}~";

        private const string FORBIDDEN = "\"*/:<>?\\|";

        private const char REPLACEMENT = '_';

        private const int BASE_LENGTH = 8;

        private const int EXTENSION_LENGTH = 3;

        private const int MAX_TAIL = 999999;

        #endregion

        #region Functions

        /// <summary>
        /// Throws unless a name can be given to a new entry.
        /// </summary>
        /// <exception cref="ArgumentException">
        /// The name is empty, longer than 255 characters, <c>.</c> or <c>..</c>, ends in a
        /// space or a period, or holds a control character or one of <c>" * / : &lt; &gt; ? \ |</c>.
        /// </exception>
        public static void Validate(string name)
        {
            ArgumentNullException.ThrowIfNull(name);

            if (name.Length is 0 or > DirectorySlot.MAX_LONG_NAME_LENGTH)
                throw new ArgumentException($"A name has 1 to {DirectorySlot.MAX_LONG_NAME_LENGTH} characters; '{name}' has {name.Length}.", nameof(name));
            if (name is "." or ".." || name[^1] is ' ' or '.')
                throw new ArgumentException($"The name '{name}' cannot end in a space or a period.", nameof(name));

            foreach (char c in name)
            {
                if (c < ' ' || FORBIDDEN.Contains(c))
                    throw new ArgumentException($"The name '{name}' holds a character FAT does not allow.", nameof(name));
            }
        }

        /// <summary>
        /// Stores a name as an 8.3 name alone, if it is one.
        /// </summary>
        /// <param name="name">A validated name.</param>
        /// <param name="shortName">Receives the 11 bytes.</param>
        /// <param name="caseFlags">Receives the flags that restore the name's case.</param>
        /// <returns>Whether the name needs no long name.</returns>
        public static bool TryFit(string name, Span<byte> shortName, out byte caseFlags)
        {
            caseFlags = 0;
            int dot = name.LastIndexOf('.');
            string stem = dot < 0 ? name : name[..dot];
            string extension = dot < 0 ? string.Empty : name[(dot + 1)..];

            if (stem.Length is 0 or > BASE_LENGTH || extension.Length > EXTENSION_LENGTH || stem.Contains('.'))
                return false;
            if (!TryCase(stem, DirectorySlot.LOWER_CASE_BASE, ref caseFlags) || !TryCase(extension, DirectorySlot.LOWER_CASE_EXTENSION, ref caseFlags))
                return false;

            Fill(shortName, UpperAscii(stem), UpperAscii(extension));
            return true;
        }

        /// <summary>
        /// Makes the alias of a name that needs a long name.
        /// </summary>
        /// <param name="name">A validated name.</param>
        /// <param name="taken">The names already in the directory, short and long, upper-cased.</param>
        /// <param name="shortName">Receives the 11 bytes.</param>
        /// <exception cref="FatException">Every numeric tail is taken.</exception>
        public static void Generate(string name, ISet<string> taken, Span<byte> shortName)
        {
            string upper = UpperAscii(name);
            int dot = upper.LastIndexOf('.');
            bool hasExtension = dot > 0 && upper[..dot].TrimStart('.').Length > 0;
            string stemSource = hasExtension ? upper[..dot] : upper;
            string extensionSource = hasExtension ? upper[(dot + 1)..] : string.Empty;

            bool isLossy = false;
            string stem = Convert(stemSource, BASE_LENGTH, ref isLossy);
            string extension = Convert(extensionSource, EXTENSION_LENGTH, ref isLossy);
            if (stem.Length == 0)
            {
                stem = REPLACEMENT.ToString();
                isLossy = true;
            }

            if (!isLossy && !taken.Contains(Join(stem, extension)))
            {
                Fill(shortName, stem, extension);
                return;
            }

            for (int tail = 1; tail <= MAX_TAIL; tail++)
            {
                string suffix = "~" + tail.ToString(CultureInfo.InvariantCulture);
                string candidate = stem[..Math.Min(stem.Length, BASE_LENGTH - suffix.Length)] + suffix;
                if (taken.Contains(Join(candidate, extension)))
                    continue;

                Fill(shortName, candidate, extension);
                return;
            }

            throw new FatException(FatErrorKind.NoSpace, $"Every 8.3 alias for '{name}' is taken.");
        }

        /// <summary>
        /// The form <see cref="ShortName.Decode(ReadOnlySpan{byte})"/> gives a stored name.
        /// </summary>
        public static string Join(string stem, string extension)
        {
            return extension.Length == 0 ? stem : stem + "." + extension;
        }

        private static string Convert(string source, int limit, ref bool isLossy)
        {
            var result = new StringBuilder(limit);
            foreach (char c in source)
            {
                if (c is ' ' or '.')
                {
                    isLossy = true;
                    continue;
                }

                if (result.Length == limit)
                {
                    isLossy = true;
                    break;
                }

                if (IsShortNameChar(c))
                {
                    result.Append(c);
                }
                else
                {
                    result.Append(REPLACEMENT);
                    isLossy = true;
                }
            }

            return result.ToString();
        }

        private static bool TryCase(string part, byte lowerFlag, ref byte caseFlags)
        {
            bool hasUpper = false;
            bool hasLower = false;
            foreach (char c in part)
            {
                if (!IsShortNameChar(UpperAscii(c)))
                    return false;
                hasUpper |= c is >= 'A' and <= 'Z';
                hasLower |= c is >= 'a' and <= 'z';
            }

            if (hasUpper && hasLower)
                return false;
            if (hasLower)
                caseFlags |= lowerFlag;
            return true;
        }

        /// <remarks>
        /// Only ASCII letters change: the invariant culture would turn a dotless i into an
        /// ASCII I, which an 8.3 name would then keep.
        /// </remarks>
        private static char UpperAscii(char c)
        {
            return c is >= 'a' and <= 'z' ? (char)(c - ('a' - 'A')) : c;
        }

        private static string UpperAscii(string text)
        {
            return string.Create(text.Length, text, (span, source) =>
            {
                for (int i = 0; i < source.Length; i++)
                    span[i] = UpperAscii(source[i]);
            });
        }

        private static bool IsShortNameChar(char c)
        {
            return c is >= 'A' and <= 'Z' or >= '0' and <= '9' || SPECIAL.Contains(c);
        }

        private static void Fill(Span<byte> shortName, string stem, string extension)
        {
            shortName[..DirectorySlot.NAME_LENGTH].Fill((byte)' ');
            for (int i = 0; i < stem.Length; i++)
                shortName[i] = (byte)stem[i];
            for (int i = 0; i < extension.Length; i++)
                shortName[BASE_LENGTH + i] = (byte)extension[i];
        }

        #endregion
    }
}
