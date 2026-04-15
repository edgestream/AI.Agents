using System.ComponentModel;
using System.Text.Json;
using Microsoft.Agents.AI;

namespace AI.MAF.Skills;

#pragma warning disable MAAI001 // AgentClassSkill is marked experimental

/// <summary>
/// A class-based agent skill for date/time operations.
/// Demonstrates the <see cref="AgentClassSkill{T}"/> pattern with resources and scripts.
/// </summary>
public sealed class DateTimeSkill : AgentClassSkill<DateTimeSkill>
{
    /// <inheritdoc />
    public override AgentSkillFrontmatter Frontmatter { get; } = new(
        "datetime",
        "Get current date/time, convert between time zones, or calculate durations. " +
        "Use when asked about time, dates, time zone conversions, or scheduling.");

    /// <inheritdoc />
    protected override string Instructions => """
        Use this skill when the user asks about the current time, dates, time zones, or scheduling.
        1. Use the timezones resource to see available time zone identifiers.
        2. Use the now script to get the current date/time in a specific time zone.
        3. Use the convert script to convert a time between zones.
        """;

    /// <summary>
    /// Provides a list of common time zone identifiers.
    /// </summary>
    [AgentSkillResource("timezones")]
    [Description("List of common time zone identifiers with their display names.")]
    public string TimeZones
    {
        get
        {
            var commonZones = new[]
            {
                "UTC", "America/New_York", "America/Los_Angeles", "America/Chicago",
                "Europe/London", "Europe/Paris", "Europe/Berlin", "Europe/Moscow",
                "Asia/Tokyo", "Asia/Shanghai", "Asia/Singapore", "Asia/Dubai",
                "Australia/Sydney", "Pacific/Auckland"
            };

            var lines = commonZones
                .Select(id =>
                {
                    try
                    {
                        var tz = TimeZoneInfo.FindSystemTimeZoneById(id);
                        return $"- {id} ({tz.DisplayName})";
                    }
                    catch
                    {
                        return $"- {id}";
                    }
                });

            return string.Join("\n", lines);
        }
    }

    /// <summary>
    /// Returns the current date and time in the specified time zone.
    /// </summary>
    [AgentSkillScript("now")]
    [Description("Returns the current date and time in the specified time zone.")]
    public static string Now(
        [Description("The time zone identifier (e.g., 'UTC', 'America/New_York', 'Europe/Berlin'). Defaults to UTC.")] 
        string timeZoneId = "UTC")
    {
        var tz = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
        
        return JsonSerializer.Serialize(new
        {
            timeZone = timeZoneId,
            dateTime = now.ToString("o"),
            formatted = now.ToString("F"),
            utcOffset = tz.GetUtcOffset(now).ToString()
        });
    }

    /// <summary>
    /// Converts a date/time from one time zone to another.
    /// </summary>
    [AgentSkillScript("convert")]
    [Description("Converts a date/time from one time zone to another.")]
    public static string Convert(
        [Description("The date/time string to convert (ISO 8601 format preferred).")] 
        string dateTime,
        [Description("The source time zone identifier.")] 
        string fromTimeZone,
        [Description("The target time zone identifier.")] 
        string toTimeZone)
    {
        var fromTz = TimeZoneInfo.FindSystemTimeZoneById(fromTimeZone);
        var toTz = TimeZoneInfo.FindSystemTimeZoneById(toTimeZone);
        var dt = DateTime.Parse(dateTime);
        var utc = TimeZoneInfo.ConvertTimeToUtc(dt, fromTz);
        var result = TimeZoneInfo.ConvertTimeFromUtc(utc, toTz);

        return JsonSerializer.Serialize(new
        {
            from = new { timeZone = fromTimeZone, dateTime = dt.ToString("o") },
            to = new { timeZone = toTimeZone, dateTime = result.ToString("o"), formatted = result.ToString("F") }
        });
    }
}

#pragma warning restore MAAI001