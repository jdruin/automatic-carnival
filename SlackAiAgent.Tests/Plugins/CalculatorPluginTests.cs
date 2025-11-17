using FluentAssertions;
using SlackAiAgent.Plugins;
using Xunit;

namespace SlackAiAgent.Tests.Plugins;

public class CalculatorPluginTests
{
    [Theory]
    [InlineData(5, 3, 8)]
    [InlineData(10.5, 4.5, 15)]
    [InlineData(-5, 3, -2)]
    [InlineData(0, 0, 0)]
    public void Add_ShouldReturnCorrectSum(double a, double b, double expected)
    {
        // Arrange
        var plugin = new CalculatorPlugin();

        // Act
        var result = plugin.Add(a, b);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(10, 3, 7)]
    [InlineData(5.5, 2.5, 3)]
    [InlineData(-5, -3, -2)]
    [InlineData(0, 5, -5)]
    public void Subtract_ShouldReturnCorrectDifference(double a, double b, double expected)
    {
        // Arrange
        var plugin = new CalculatorPlugin();

        // Act
        var result = plugin.Subtract(a, b);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(5, 3, 15)]
    [InlineData(10.5, 2, 21)]
    [InlineData(-5, 3, -15)]
    [InlineData(0, 100, 0)]
    public void Multiply_ShouldReturnCorrectProduct(double a, double b, double expected)
    {
        // Arrange
        var plugin = new CalculatorPlugin();

        // Act
        var result = plugin.Multiply(a, b);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(10, 2, "5")]
    [InlineData(15, 3, "5")]
    [InlineData(7, 2, "3.5")]
    public void Divide_ShouldReturnCorrectQuotient(double a, double b, string expected)
    {
        // Arrange
        var plugin = new CalculatorPlugin();

        // Act
        var result = plugin.Divide(a, b);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void Divide_ShouldReturnError_WhenDividingByZero()
    {
        // Arrange
        var plugin = new CalculatorPlugin();

        // Act
        var result = plugin.Divide(10, 0);

        // Assert
        result.Should().Contain("Error");
        result.Should().Contain("divide by zero");
    }

    [Theory]
    [InlineData(2, 3, 8)]
    [InlineData(5, 2, 25)]
    [InlineData(10, 0, 1)]
    [InlineData(2, -1, 0.5)]
    public void Power_ShouldReturnCorrectResult(double baseNumber, double exponent, double expected)
    {
        // Arrange
        var plugin = new CalculatorPlugin();

        // Act
        var result = plugin.Power(baseNumber, exponent);

        // Assert
        result.Should().BeApproximately(expected, 0.0001);
    }

    [Theory]
    [InlineData(9, "3")]
    [InlineData(16, "4")]
    [InlineData(2, "1.4142135623730951")]
    public void SquareRoot_ShouldReturnCorrectResult(double number, string expected)
    {
        // Arrange
        var plugin = new CalculatorPlugin();

        // Act
        var result = plugin.SquareRoot(number);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void SquareRoot_ShouldReturnError_WhenNumberIsNegative()
    {
        // Arrange
        var plugin = new CalculatorPlugin();

        // Act
        var result = plugin.SquareRoot(-9);

        // Assert
        result.Should().Contain("Error");
        result.Should().Contain("negative number");
    }

    [Theory]
    [InlineData(50, 200, "25.00%")]
    [InlineData(25, 100, "25.00%")]
    [InlineData(75, 150, "50.00%")]
    [InlineData(1, 3, "33.33%")]
    public void Percentage_ShouldReturnCorrectPercentage(double part, double whole, string expected)
    {
        // Arrange
        var plugin = new CalculatorPlugin();

        // Act
        var result = plugin.Percentage(part, whole);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void Percentage_ShouldReturnError_WhenWholeIsZero()
    {
        // Arrange
        var plugin = new CalculatorPlugin();

        // Act
        var result = plugin.Percentage(50, 0);

        // Assert
        result.Should().Contain("Error");
        result.Should().Contain("zero");
    }
}
