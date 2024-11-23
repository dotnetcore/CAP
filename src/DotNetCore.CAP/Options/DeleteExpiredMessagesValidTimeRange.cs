using System;

namespace DotNetCore.CAP.Options
{
    public class DeleteExpiredMessagesValidTimeRange
    {
        public DeleteExpiredMessagesValidTimeRange(TimeSpan fromHour, TimeSpan toHour)
        {
            FromHour = fromHour;
            ToHour = toHour;

            ThrowExceptionIfTimeRangeIsNotValid();
        }

        public TimeSpan FromHour { get; private set; }
        public TimeSpan ToHour { get; private set; }

        void ThrowExceptionIfTimeRangeIsNotValid()
        {
            if (FromHour >= ToHour)
                throw new ArgumentException("FromHour must be less than ToHour.");
        }
    }

    public static class DeleteExpiredMessagesValidTimeRangeExtension
    {
        /// <summary>
        /// Checks if the current time of day falls within the valid range.
        /// </summary>
        /// <param name="timeRange">The time range to check against.</param>
        /// <returns>Returns true if the current time is within the valid range; otherwise, false.</returns>
        public static bool CurrentTimeIsValid(this DeleteExpiredMessagesValidTimeRange timeRange)
        {
            var now = DateTime.Now.TimeOfDay;
            return now >= timeRange.FromHour && now <= timeRange.ToHour;
        }
    }
}
