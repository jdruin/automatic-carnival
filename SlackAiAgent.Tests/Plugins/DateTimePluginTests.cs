using FluentAssertions;
using SlackAiAgent.Plugins;
using Xunit;

namespace SlackAiAgent.Tests.Plugins;

public class DateTimePluginTests
{
    [Fact]
    public void GetCurrentTime_ShouldReturnUtcTime_WhenNoTimezoneSpecified()
    {
        // Arrange
        var plugin = new DateTimePlugin();

        // Act
        var result = plugin.GetCurrentTime(null);

        // Assert
        result.Should().Contain("UTC");
        result.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void GetCurrentTime_ShouldReturnSpecifiedTimezone()
    {
        // Arrange
        var plugin = new DateTimePlugin();

        // Act
        var result = plugin.GetCurrentTime("America/New_York");

        // Assert
        result.Should().Contain("Eastern");
        result.Should().MatchRegex(@"\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}");
    }

    [Fact]
    public void GetCurrentTime_ShouldReturnError_WhenTimezoneInvalid()
    {
        // Arrange
        var plugin = new DateTimePlugin();

        // Act
        var result = plugin.GetCurrentTime("Invalid/Timezone");

        // Assert
        result.Should().Contain("Error");
        result.Should().Contain("not found");
    }

    [Fact]
    public void GetCurrentDate_ShouldReturnDateInCorrectFormat()
    {
        // Arrange
        var plugin = new DateTimePlugin();

        // Act
        var result = plugin.GetCurrentDate();

        // Assert
        result.Should().Contain("Current date");
        result.Should().MatchRegex(@"\d{4}-\d{2}-\d{2}");
    }

    [Fact]
    public void GetDayOfWeek_ShouldReturnTodayDayOfWeek_WhenNoDateProvided()
    {
        // Arrange
        var plugin = new DateTimePlugin();
        var today = DateTime.UtcNow;

        // Act
        var result = plugin.GetDayOfWeek(null);

        // Assert
        result.Should().Contain(today.DayOfWeek.ToString());
    }

    [Fact]
    public void GetDayOfWeek_ShouldReturnCorrectDay_ForSpecificDate()
    {
        // Arrange
        var plugin = new DateTimePlugin();
        // 2025-01-01 was a Wednesday
        var date = "2025-01-01";

        // Act
        var result = plugin.GetDayOfWeek(date);

        // Assert
        result.Should().Contain("Wednesday");
        result.Should().Contain(date);
    }

    [Fact]
    public void GetDayOfWeek_ShouldReturnError_WhenDateFormatInvalid()
    {
        // Arrange
        var plugin = new DateTimePlugin();

        // Act
        var result = plugin.GetDayOfWeek("invalid-date");

        // Assert
        result.Should().Contain("Error");
        result.Should().Contain("Invalid date format");
    }

    [Fact]
    public void CalculateDateDifference_ShouldReturnCorrectDifference()
    {
        // Arrange
        var plugin = new DateTimePlugin();

        // Act
        var result = plugin.CalculateDateDifference("2025-01-01", "2025-01-11");

        // Assert
        result.Should().Contain("10 days");
    }

    [Fact]
    public void CalculateDateDifference_ShouldReturnAbsoluteDifference_WhenDatesReversed()
    {
        // Arrange
        var plugin = new DateTimePlugin();

        // Act
        var result = plugin.CalculateDateDifference("2025-01-11", "2025-01-01");

        // Assert
        result.Should().Contain("10 days");
    }

    [Fact]
    public void CalculateDateDifference_ShouldReturnError_WhenDateFormatInvalid()
    {
        // Arrange
        var plugin = new DateTimePlugin();

        // Act
        var result = plugin.CalculateDateDifference("invalid", "2025-01-01");

        // Assert
        result.Should().Contain("Error");
        result.Should().Contain("Invalid date format");
    }
}
