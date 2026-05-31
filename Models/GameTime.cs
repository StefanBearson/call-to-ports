namespace porter_of_call.Models;

public static class GameTime
{
    public const double HoursPerRealMinute = HoursPerDay;
    public const double HoursPerDay = 24.0;

    public static double ToGameHours(TimeSpan elapsedRealTime) =>
        elapsedRealTime.TotalMinutes * HoursPerRealMinute;

    public static string FormatDuration(double hours)
    {
        if (hours <= 0)
            return "arriving";

        int roundedHours = Math.Max(1, (int)Math.Ceiling(hours));
        int days = roundedHours / 24;
        int remainingHours = roundedHours % 24;

        return (days, remainingHours) switch
        {
            (0, _) => $"{remainingHours}h",
            (_, 0) => $"{days}d",
            _ => $"{days}d {remainingHours}h",
        };
    }
}
