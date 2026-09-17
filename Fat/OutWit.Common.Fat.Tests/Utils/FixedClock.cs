namespace OutWit.Common.Fat.Tests.Utils
{
    /// <summary>
    /// A clock that shows a set time, in a zone without offset, so local time is the time set.
    /// </summary>
    internal sealed class FixedClock : TimeProvider
    {
        #region Constructors

        public FixedClock(DateTime now)
        {
            Now = now;
        }

        #endregion

        #region Functions

        public override DateTimeOffset GetUtcNow()
        {
            return new DateTimeOffset(DateTime.SpecifyKind(Now, DateTimeKind.Utc));
        }

        #endregion

        #region Properties

        public DateTime Now { get; set; }

        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;

        #endregion
    }
}
