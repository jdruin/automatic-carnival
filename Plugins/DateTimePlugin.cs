using System.ComponentModel;
using Microsoft.SemanticKernel;

namespace SlackAiAgent.Plugins;

/// <summary>
/// Plugin that provides date and time information
/// </summary>
public class DateTimePlugin
{
    [KernelFunction("get_current_time")]
    [Description("Gets the current time in a specific timezone. If no timezone is specified, returns UTC time.")]
    public string GetCurrentTime(
        [Description("The timezone (e.g., 'UTC', 'America/New_York', 'Europe/London'). Defaults to UTC.")]
        string? timezone = null)
    {
        try
        {
            var timeZoneInfo = string.IsNullOrWhiteSpace(timezone)
                ? TimeZoneInfo.Utc
                : TimeZoneInfo.FindSystemTimeZoneById(timezone);

            var time = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZoneInfo);
            return $"Current time in {timeZoneInfo.DisplayName}: {time:yyyy-MM-dd HH:mm:ss}";
        }
        catch (TimeZoneNotFoundException)
        {
            return $"Error: Timezone '{timezone}' not found. Using UTC: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}";
        }
    }

    [KernelFunction("get_current_date")]
    [Description("Gets the current date")]
    public string GetCurrentDate()
    {
        return $"Current date (UTC): {DateTime.UtcNow:yyyy-MM-dd}";
    }

    [KernelFunction("get_day_of_week")]
    [Description("Gets the day of the week for today or a specific date")]
    public string GetDayOfWeek(
        [Description("Optional date in format yyyy-MM-dd. If not provided, uses today's date.")]
        string? date = null)
    {
        try
        {
            var targetDate = string.IsNullOrWhiteSpace(date)
                ? DateTime.UtcNow
                : DateTime.Parse(date);

            return $"{targetDate:yyyy-MM-dd} is a {targetDate.DayOfWeek}";
        }
        catch (FormatException)
        {
            return $"Error: Invalid date format. Please use yyyy-MM-dd format.";
        }
    }

    [KernelFunction("calculate_date_difference")]
    [Description("Calculates the number of days between two dates")]
    public string CalculateDateDifference(
        [Description("Start date in format yyyy-MM-dd")] string startDate,
        [Description("End date in format yyyy-MM-dd")] string endDate)
    {
        try
        {
            var start = DateTime.Parse(startDate);
            var end = DateTime.Parse(endDate);
            var difference = (end - start).Days;

            return $"Days between {startDate} and {endDate}: {Math.Abs(difference)} days";
        }
        catch (FormatException)
        {
            return "Error: Invalid date format. Please use yyyy-MM-dd format.";
        }
    }
}
