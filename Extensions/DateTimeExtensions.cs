namespace MRIV.Extensions
{
    // Extensions/DateTimeExtensions.cs
    public static class DateTimeExtensions
    {
        public static string ToRelativeTimeString(this DateTime dateTime)
        {
            var ts = DateTime.Now - dateTime;

            if (ts.TotalMinutes < 1)
                return "just now";
            if (ts.TotalMinutes < 60)
                return $"{(int)ts.TotalMinutes} min ago";
            if (ts.TotalHours < 24)
                return $"{(int)ts.TotalHours} hours ago";
            if (ts.TotalDays < 7)
                return $"{(int)ts.TotalDays} days ago";

            return dateTime.ToString("MMM dd, yyyy");
        }
    }
}
