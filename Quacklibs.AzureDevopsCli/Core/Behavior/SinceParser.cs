using System.CommandLine.Completions;
using System.Globalization;
using System.IO;

namespace Quacklibs.AzureDevopsCli.Core.Behavior;

public class SinceCompletionItem : CompletionItem
{
    public SinceCompletionItem(string label, string kind = "Value", string? sortText = null, string? insertText = null, string? documentation = null, string? detail = null) : base(label, kind, sortText, insertText, documentation, detail)
    {
    }
}

public static class SinceParser
{
    private static readonly List<string> inputOptions = new()
    {
        "today",
        "yesterday",
        "monday",
        "tuesday",
        "wednesday",
        "thursday",
        "friday",
        "saturday",
        "sunday",
        "thisweek",
        "lastweek",
        "thismonth",
        "nd",
        "dd-MM-yyyy",
        "yyyy-MM-dd"
    };

    public static DateTimeRangeType ToDateTimeRange(this SinceType since)
    {
        var now = DateTime.Now;

        var fixedSince = since.Value.ToLowerInvariant() switch
        {
            "today" => new DateTimeRangeType(from: now.Date, till: now),
            "yesterday" => new DateTimeRangeType(from: now.Date.AddDays(-1), till: now),
            "monday" => FromDayOfWeek(now, DayOfWeek.Monday),
            "tuesday" => FromDayOfWeek(now, DayOfWeek.Tuesday),
            "wednesday" => FromDayOfWeek(now, DayOfWeek.Wednesday),
            "thursday" => FromDayOfWeek(now, DayOfWeek.Thursday),
            "friday" => FromDayOfWeek(now, DayOfWeek.Friday),
            "saturday" => FromDayOfWeek(now, DayOfWeek.Saturday),
            "sunday" => FromDayOfWeek(now, DayOfWeek.Sunday),
            "thisweek" => new DateTimeRangeType(from: now.Date.AddDays(-(int)now.DayOfWeek), till: now),
            "lastweek" => new DateTimeRangeType(from: now.Date.AddDays(-(int)now.DayOfWeek - 7), till: now.Date.AddDays(-(int)now.DayOfWeek)),
            "thismonth" => new DateTimeRangeType(from: new DateTime(now.Year, now.Month, 1), till: now),
            _ => null
        };

        if (fixedSince != null)
            return fixedSince;

        // relatieve periodes: 3d
        if (since.Value.EndsWith("d", StringComparison.InvariantCultureIgnoreCase) && int.TryParse(since.Value[..^1], out var days))
        {
            return new DateTimeRangeType(from: now.Date.AddDays(-days), till: now);
        }

        // datum in dd-MM-yyyy of yyyy-MM-dd
        if (DateTime.TryParseExact(since.Value, "dd-MM-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate) || DateTime.TryParse(since.Value, out parsedDate))
            return new DateTimeRangeType(from: parsedDate, till: now);

        throw new ArgumentException($"Invalid value for since value '{since.Value}'. \n" +
                                    "Valid options include: \n" +
                                    "  fixed periods: today, yesterday, monday, tuesday, ..., thisweek, lastweek, thismonth \n" +
                                    "  relative periods: 1d, 3d, 2w, ... \n" +
                                    "  Dates: dd-MM-yyyy or yyyy-MM-dd");
    }

    private static DateTimeRangeType FromDayOfWeek(DateTime now, DayOfWeek day)
    {
        var from = now.Date.AddDays(-(now.DayOfWeek - day));
        return new DateTimeRangeType(from: from, till: now);
    }

    public static IEnumerable<SinceCompletionItem> ToCompletionOptions()
    {
        return inputOptions
            .Select(label => new SinceCompletionItem(
                label: label,
                kind: "Value",
                documentation: GetDocumentation(label)
            ));
    }

    // Optional: human-readable hint/documentation for each completion
    private static string GetDocumentation(string label) => label switch
    {
        "dd-MM-yyyy" => "Specify a date in day-month-year format",
        "yyyy-MM-dd" => "Specify a date in ISO format",
        _ => $"Fixed anchor: {label}"
    };
}