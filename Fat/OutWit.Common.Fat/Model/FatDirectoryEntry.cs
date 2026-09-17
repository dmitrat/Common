using System;
using OutWit.Common.Abstract;
using OutWit.Common.Attributes;
using OutWit.Common.Fat.Directories;
using OutWit.Common.Values;

namespace OutWit.Common.Fat.Model
{
    /// <summary>
    /// A file or directory on a FAT or exFAT volume.
    /// </summary>
    /// <remarks>
    /// <para>
    /// FAT12/16/32 store times as local wall-clock time without a zone, so their times are
    /// of <see cref="DateTimeKind.Unspecified"/> kind. exFAT records each time's offset from
    /// UTC; a time whose offset is recorded is given in UTC, of
    /// <see cref="DateTimeKind.Utc"/> kind, and one whose offset is not is given as stored.
    /// </para>
    /// <para>
    /// A time that is absent or not a valid date is <c>null</c>.
    /// </para>
    /// </remarks>
    public sealed class FatDirectoryEntry : ModelBase
    {
        #region Model Base

        /// <inheritdoc />
        public override bool Is(ModelBase modelBase, double tolerance = DEFAULT_TOLERANCE)
        {
            if (modelBase is not FatDirectoryEntry other)
                return false;

            return Path.Is(other.Path)
                   && Name.Is(other.Name)
                   && ShortName.Is(other.ShortName)
                   && Attributes.Is(other.Attributes)
                   && Length.Is(other.Length)
                   && FirstCluster.Is(other.FirstCluster)
                   && Created.Is(other.Created)
                   && Modified.Is(other.Modified)
                   && Accessed.Is(other.Accessed);
        }

        /// <inheritdoc />
        public override FatDirectoryEntry Clone()
        {
            return new FatDirectoryEntry
            {
                Path = Path,
                Name = Name,
                ShortName = ShortName,
                Attributes = Attributes,
                Length = Length,
                FirstCluster = FirstCluster,
                Created = Created,
                Modified = Modified,
                Accessed = Accessed
            };
        }

        #endregion

        #region Properties

        /// <summary>
        /// The full path from the root, with '/' between components; "/" for the root.
        /// </summary>
        [ToString]
        public string Path { get; init; } = "/";

        /// <summary>
        /// The long name, or the short name as displayed when there is no long name.
        /// </summary>
        public string Name { get; init; } = string.Empty;

        /// <summary>
        /// The 8.3 name as stored, such as <c>LONGFI~1.TXT</c>; empty for the root, and on
        /// exFAT, which has none.
        /// </summary>
        public string ShortName { get; init; } = string.Empty;

        /// <summary>
        /// The attribute bits.
        /// </summary>
        [ToString]
        public FatAttributes Attributes { get; init; }

        /// <summary>
        /// The length of a file in bytes; zero for a directory.
        /// </summary>
        [ToString]
        public long Length { get; init; }

        /// <summary>
        /// The first cluster of the entry's data, or zero when it has none.
        /// </summary>
        public uint FirstCluster { get; init; }

        /// <summary>
        /// When the entry was created, to 10 ms.
        /// </summary>
        public DateTime? Created { get; init; }

        /// <summary>
        /// When the entry was last written: to two seconds, on exFAT to 10 ms.
        /// </summary>
        [ToString]
        public DateTime? Modified { get; init; }

        /// <summary>
        /// When the entry was last accessed: the day, on exFAT to two seconds.
        /// </summary>
        public DateTime? Accessed { get; init; }

        /// <summary>
        /// Whether the entry is a directory.
        /// </summary>
        public bool IsDirectory => (Attributes & FatAttributes.Directory) != 0;

        #endregion
    }
}
