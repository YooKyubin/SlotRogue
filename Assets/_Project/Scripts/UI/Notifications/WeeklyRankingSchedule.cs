using System;

namespace SlotRogue.UI.Notifications
{
    public static class WeeklyRankingSchedule
    {
        public static readonly TimeSpan KoreaUtcOffset = TimeSpan.FromHours(9);
        public static readonly TimeSpan DefaultDeadlineLeadTime =
            TimeSpan.FromHours(3);

        public static DateTime GetNextResetUtc(DateTime utcNow)
        {
            DateTime normalizedUtc = NormalizeUtc(utcNow);
            DateTime koreaNow = normalizedUtc.Add(KoreaUtcOffset);
            int daysUntilWednesday =
                ((int)DayOfWeek.Wednesday - (int)koreaNow.DayOfWeek + 7) % 7;
            DateTime resetKorea = koreaNow.Date.AddDays(daysUntilWednesday);

            if (resetKorea <= koreaNow)
            {
                resetKorea = resetKorea.AddDays(7);
            }

            return DateTime.SpecifyKind(
                resetKorea.Subtract(KoreaUtcOffset),
                DateTimeKind.Utc);
        }

        public static DateTime GetNextDeadlineUtc(
            DateTime utcNow,
            TimeSpan deadlineLeadTime)
        {
            DateTime normalizedUtc = NormalizeUtc(utcNow);
            TimeSpan normalizedLeadTime =
                deadlineLeadTime < TimeSpan.Zero
                    ? TimeSpan.Zero
                    : deadlineLeadTime;
            DateTime resetUtc = GetNextResetUtc(normalizedUtc);
            DateTime deadlineUtc = resetUtc.Subtract(normalizedLeadTime);

            if (deadlineUtc <= normalizedUtc)
            {
                deadlineUtc = deadlineUtc.AddDays(7);
            }

            return deadlineUtc;
        }

        private static DateTime NormalizeUtc(DateTime value)
        {
            return value.Kind switch
            {
                DateTimeKind.Utc => value,
                DateTimeKind.Local => value.ToUniversalTime(),
                _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
            };
        }
    }
}
