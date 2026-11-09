namespace RaceDay.Web.Services;

/// <summary>Small display helpers shared by the views.</summary>
public static class Format
{
    /// <summary>1 becomes 1st, 2 becomes 2nd, 47 becomes 47th.</summary>
    public static string Ordinal(int number)
    {
        var lastTwo = number % 100;
        if (lastTwo is >= 11 and <= 13) return $"{number}th";
        return (number % 10) switch
        {
            1 => $"{number}st",
            2 => $"{number}nd",
            3 => $"{number}rd",
            _ => $"{number}th"
        };
    }

    public static string StatusClass(string status) =>
        status == "Confirmed" ? "status-confirmed" : "status-pending";

    public static string TypeEmoji(string eventType) => eventType switch
    {
        "Run" => "🏃",
        "Walk" => "🚶",
        "Cycle" => "🚴",
        _ => "🏁"
    };

    public static string ShortDate(DateTime date) => date.ToString("ddd, d MMM yyyy");
    public static string LongDate(DateTime date) => date.ToString("ddd, d MMM yyyy · HH:mm");
    public static string Distance(decimal km) => $"{km:0.##} km";
}
