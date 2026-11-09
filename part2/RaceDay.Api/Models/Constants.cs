namespace RaceDay.Api.Models;

public static class Roles
{
    public const string Organiser = "Organiser";
    public const string Participant = "Participant";

    public static bool TryNormalize(string? value, out string role)
    {
        role = string.Empty;
        if (string.Equals(value?.Trim(), Organiser, StringComparison.OrdinalIgnoreCase)) { role = Organiser; return true; }
        if (string.Equals(value?.Trim(), Participant, StringComparison.OrdinalIgnoreCase)) { role = Participant; return true; }
        return false;
    }
}

public static class EventTypes
{
    public const string Run = "Run";
    public const string Walk = "Walk";
    public const string Cycle = "Cycle";

    public static bool TryNormalize(string? value, out string type)
    {
        type = string.Empty;
        foreach (var candidate in new[] { Run, Walk, Cycle })
        {
            if (string.Equals(value?.Trim(), candidate, StringComparison.OrdinalIgnoreCase))
            {
                type = candidate;
                return true;
            }
        }
        return false;
    }
}

public static class EnrolmentStatuses
{
    public const string Pending = "Pending";
    public const string Confirmed = "Confirmed";

    public static bool TryNormalize(string? value, out string status)
    {
        status = string.Empty;
        if (string.Equals(value?.Trim(), Pending, StringComparison.OrdinalIgnoreCase)) { status = Pending; return true; }
        if (string.Equals(value?.Trim(), Confirmed, StringComparison.OrdinalIgnoreCase)) { status = Confirmed; return true; }
        return false;
    }
}
