using System.Text.Json;
using AI.Agents.Microsoft.Skills;

namespace AI.AGUI.Server.IntegrationTests;

/// <summary>
/// Unit tests for <see cref="DateTimeSkill"/>.
/// </summary>
[TestClass]
public sealed class DateTimeSkillTests
{
    /// <summary>
    /// The <c>now</c> script returns valid JSON with expected properties for UTC.
    /// </summary>
    [TestMethod]
    public void Now_ReturnsValidJson_ForUtc()
    {
        var result = DateTimeSkill.Now("UTC");

        var json = JsonDocument.Parse(result);
        Assert.AreEqual("UTC", json.RootElement.GetProperty("timeZone").GetString());
        Assert.IsTrue(json.RootElement.TryGetProperty("dateTime", out _));
        Assert.IsTrue(json.RootElement.TryGetProperty("formatted", out _));
        Assert.IsTrue(json.RootElement.TryGetProperty("utcOffset", out _));
    }

    /// <summary>
    /// The <c>now</c> script handles different time zones correctly.
    /// </summary>
    [TestMethod]
    [DataRow("America/New_York")]
    [DataRow("Europe/Berlin")]
    [DataRow("Asia/Tokyo")]
    public void Now_ReturnsValidJson_ForDifferentTimeZones(string timeZoneId)
    {
        var result = DateTimeSkill.Now(timeZoneId);

        var json = JsonDocument.Parse(result);
        Assert.AreEqual(timeZoneId, json.RootElement.GetProperty("timeZone").GetString());
    }

    /// <summary>
    /// The <c>convert</c> script correctly converts times between zones.
    /// </summary>
    [TestMethod]
    public void Convert_CorrectlyConvertsTimeZones()
    {
        var result = DateTimeSkill.Convert("2026-04-15T12:00:00", "UTC", "America/New_York");

        var json = JsonDocument.Parse(result);
        Assert.AreEqual("UTC", json.RootElement.GetProperty("from").GetProperty("timeZone").GetString());
        Assert.AreEqual("America/New_York", json.RootElement.GetProperty("to").GetProperty("timeZone").GetString());
        
        // Verify the conversion is reasonable (UTC 12:00 -> EST/EDT is 7:00/8:00 AM)
        var toDateTime = DateTime.Parse(json.RootElement.GetProperty("to").GetProperty("dateTime").GetString()!);
        Assert.IsTrue(toDateTime.Hour >= 7 && toDateTime.Hour <= 8, $"Expected hour between 7-8, got {toDateTime.Hour}");
    }

    /// <summary>
    /// The <c>timezones</c> resource returns a non-empty list of time zones.
    /// </summary>
    [TestMethod]
    public void TimeZones_ReturnsNonEmptyList()
    {
        var skill = new DateTimeSkill();

        var result = skill.TimeZones;

        Assert.IsFalse(string.IsNullOrWhiteSpace(result));
        Assert.IsTrue(result.Contains("UTC"));
        Assert.IsTrue(result.Contains("America/New_York") || result.Contains("Eastern"));
    }

    /// <summary>
    /// The skill frontmatter has expected name and description.
    /// </summary>
    [TestMethod]
    public void Frontmatter_HasExpectedValues()
    {
        var skill = new DateTimeSkill();

        Assert.AreEqual("datetime", skill.Frontmatter.Name);
        Assert.IsTrue(skill.Frontmatter.Description.Contains("time zone", StringComparison.OrdinalIgnoreCase));
    }
}
