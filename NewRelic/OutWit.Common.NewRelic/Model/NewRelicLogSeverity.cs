using System.Collections.Generic;
using OutWit.Common.Enums;

namespace OutWit.Common.NewRelic.Model
{
    public sealed class NewRelicLogSeverity : StringEnum<NewRelicLogSeverity>
    {
        #region Static Constants
        
        public static readonly NewRelicLogSeverity Trace = new("Trace", 0);

        public static readonly NewRelicLogSeverity Debug = new("Debug", 1);

        public static readonly NewRelicLogSeverity Information = new("Information", 2);

        public static readonly NewRelicLogSeverity Warning = new("Warning", 3);

        public static readonly NewRelicLogSeverity Error = new("Error", 4);

        public static readonly NewRelicLogSeverity Critical = new("Critical", 5);

        public static readonly NewRelicLogSeverity Fatal = new("Fatal", 6);

        #endregion

        #region Constructors

        private NewRelicLogSeverity(string value, int level) : base(value)
        {
            Level = level;
        }

        #endregion

        #region Functions

        /// <summary>
        /// Returns all severity levels at or above the specified minimum level.
        /// </summary>
        /// <param name="minLevel">The minimum severity level.</param>
        /// <returns>A list of severity levels with a numeric level greater than or equal to <paramref name="minLevel"/>.</returns>
        public static IReadOnlyList<NewRelicLogSeverity> LevelAtLeast(NewRelicLogSeverity minLevel)
        {
            var levels = new List<NewRelicLogSeverity>();
            foreach (var severity in GetAll())
            {
                if (severity.Level >= minLevel.Level)
                    levels.Add(severity);
            }
            return levels;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the numeric severity level (higher = more severe).
        /// </summary>
        public int Level { get; }

        #endregion
    }
}
