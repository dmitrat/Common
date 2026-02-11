using MemoryPack;
using OutWit.Common.Abstract;
using OutWit.Common.Values;

namespace OutWit.Common.Settings.Providers
{
    /// <summary>
    /// Represents metadata for a settings group (priority, display name).
    /// </summary>
    [MemoryPackable]
    public sealed partial class SettingsGroupInfo : ModelBase
    {
        #region Constructors

        public SettingsGroupInfo()
        {
            Group = "";
            DisplayName = "";
        }

        #endregion

        #region Functions

        public override string ToString()
        {
            return $"{Group} (Priority={Priority}, DisplayName={DisplayName})";
        }

        #endregion

        #region ModelBase

        public override bool Is(ModelBase modelBase, double tolerance = DEFAULT_TOLERANCE)
        {
            if (modelBase is not SettingsGroupInfo other)
                return false;

            return Group.Is(other.Group) &&
                   DisplayName.Is(other.DisplayName) &&
                   Priority.Is(other.Priority);
        }

        public override ModelBase Clone()
        {
            return new SettingsGroupInfo
            {
                Group = Group,
                DisplayName = DisplayName,
                Priority = Priority
            };
        }

        #endregion

        #region Properties

        public string Group { get; set; }

        public string DisplayName { get; set; }

        public int Priority { get; set; }

        #endregion
    }
}
