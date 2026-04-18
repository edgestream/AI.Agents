using AI.Agents.Microsoft.Client;
using Microsoft.Extensions.AI;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AI.Agents.Server.Tests;

[TestClass]
public sealed class TokenUsageTrackingTests
{
    [TestMethod]
    public void TokenUsageStore_RecordsUsage()
    {
        // Arrange
        var store = new TokenUsageStore();
        var usage = new UsageDetails
        {
            InputTokenCount = 100,
            OutputTokenCount = 50,
            TotalTokenCount = 150
        };

        // Act
        store.Record(usage);

        // Assert
        var latest = store.GetLatest();
        Assert.IsNotNull(latest);
        Assert.AreEqual(100, latest.InputTokens);
        Assert.AreEqual(50, latest.OutputTokens);
        Assert.AreEqual(150, latest.TotalTokens);
    }

    [TestMethod]
    public void TokenUsageStore_GetRecent_ReturnsInReverseOrder()
    {
        // Arrange
        var store = new TokenUsageStore();
        
        // Record multiple usage events
        for (int i = 1; i <= 5; i++)
        {
            var usage = new UsageDetails
            {
                InputTokenCount = i * 10,
                OutputTokenCount = i * 5,
                TotalTokenCount = i * 15
            };
            store.Record(usage);
            Thread.Sleep(10); // Ensure different timestamps
        }

        // Act
        var recent = store.GetRecent(3).ToList();

        // Assert
        Assert.AreEqual(3, recent.Count);
        // Most recent should be first (reverse order)
        Assert.AreEqual(50, recent[0].InputTokens);
        Assert.AreEqual(40, recent[1].InputTokens);
        Assert.AreEqual(30, recent[2].InputTokens);
    }

    [TestMethod]
    public void TokenUsageStore_LimitsQueueSize()
    {
        // Arrange
        var store = new TokenUsageStore();
        
        // Record 105 events (max is 100)
        for (int i = 1; i <= 105; i++)
        {
            var usage = new UsageDetails
            {
                InputTokenCount = i,
                OutputTokenCount = i,
                TotalTokenCount = i * 2
            };
            store.Record(usage);
        }

        // Act
        var all = store.GetRecent(105).ToList();

        // Assert
        // Should only have 100 records, and should be the most recent ones
        Assert.AreEqual(100, all.Count);
        Assert.AreEqual(105, all[0].InputTokens); // Most recent
        Assert.AreEqual(6, all[99].InputTokens); // Oldest (first 5 should be dropped)
    }
}
