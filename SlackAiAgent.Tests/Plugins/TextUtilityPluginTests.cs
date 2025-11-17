using FluentAssertions;
using SlackAiAgent.Plugins;
using Xunit;

namespace SlackAiAgent.Tests.Plugins;

public class TextUtilityPluginTests
{
    [Theory]
    [InlineData("hello world", 2)]
    [InlineData("one two three four", 4)]
    [InlineData("", 0)]
    [InlineData("single", 1)]
    [InlineData("  multiple   spaces   between  ", 4)]
    public void CountWords_ShouldReturnCorrectCount(string text, int expected)
    {
        // Arrange
        var plugin = new TextUtilityPlugin();

        // Act
        var result = plugin.CountWords(text);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("hello", "olleh")]
    [InlineData("racecar", "racecar")]
    [InlineData("Hello World", "dlroW olleH")]
    [InlineData("", "")]
    public void ReverseText_ShouldReverseCorrectly(string text, string expected)
    {
        // Arrange
        var plugin = new TextUtilityPlugin();

        // Act
        var result = plugin.ReverseText(text);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("hello", "HELLO")]
    [InlineData("Hello World", "HELLO WORLD")]
    [InlineData("ALREADY UPPER", "ALREADY UPPER")]
    [InlineData("", "")]
    public void ToUppercase_ShouldConvertCorrectly(string text, string expected)
    {
        // Arrange
        var plugin = new TextUtilityPlugin();

        // Act
        var result = plugin.ToUppercase(text);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("HELLO", "hello")]
    [InlineData("Hello World", "hello world")]
    [InlineData("already lower", "already lower")]
    [InlineData("", "")]
    public void ToLowercase_ShouldConvertCorrectly(string text, string expected)
    {
        // Arrange
        var plugin = new TextUtilityPlugin();

        // Act
        var result = plugin.ToLowercase(text);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("hello world", "Hello World")]
    [InlineData("HELLO WORLD", "Hello World")]
    [InlineData("hELLO wORLD", "Hello World")]
    [InlineData("", "")]
    public void ToTitleCase_ShouldConvertCorrectly(string text, string expected)
    {
        // Arrange
        var plugin = new TextUtilityPlugin();

        // Act
        var result = plugin.ToTitleCase(text);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void EncodeBase64_ShouldEncodeCorrectly()
    {
        // Arrange
        var plugin = new TextUtilityPlugin();
        var text = "Hello World";

        // Act
        var result = plugin.EncodeBase64(text);

        // Assert
        result.Should().Be("SGVsbG8gV29ybGQ=");
    }

    [Fact]
    public void DecodeBase64_ShouldDecodeCorrectly()
    {
        // Arrange
        var plugin = new TextUtilityPlugin();
        var base64 = "SGVsbG8gV29ybGQ=";

        // Act
        var result = plugin.DecodeBase64(base64);

        // Assert
        result.Should().Be("Hello World");
    }

    [Fact]
    public void DecodeBase64_ShouldReturnError_WhenInvalidBase64()
    {
        // Arrange
        var plugin = new TextUtilityPlugin();
        var invalidBase64 = "Not-Valid-Base64!@#$";

        // Act
        var result = plugin.DecodeBase64(invalidBase64);

        // Assert
        result.Should().Contain("Error");
        result.Should().Contain("Invalid Base64");
    }

    [Fact]
    public void EncodeAndDecode_ShouldRoundTrip()
    {
        // Arrange
        var plugin = new TextUtilityPlugin();
        var originalText = "This is a test message with special chars: @#$%^&*()";

        // Act
        var encoded = plugin.EncodeBase64(originalText);
        var decoded = plugin.DecodeBase64(encoded);

        // Assert
        decoded.Should().Be(originalText);
    }

    [Theory]
    [InlineData(5)]
    [InlineData(10)]
    [InlineData(20)]
    [InlineData(1)]
    public void GenerateRandomString_ShouldGenerateCorrectLength(int length)
    {
        // Arrange
        var plugin = new TextUtilityPlugin();

        // Act
        var result = plugin.GenerateRandomString(length);

        // Assert
        result.Should().HaveLength(length);
        result.Should().MatchRegex("^[A-Za-z0-9]+$");
    }

    [Fact]
    public void GenerateRandomString_ShouldGenerateDifferentStrings()
    {
        // Arrange
        var plugin = new TextUtilityPlugin();

        // Act
        var result1 = plugin.GenerateRandomString(20);
        var result2 = plugin.GenerateRandomString(20);

        // Assert
        result1.Should().NotBe(result2);
    }

    [Fact]
    public void GenerateRandomString_ShouldUseDefaultLength_WhenNotSpecified()
    {
        // Arrange
        var plugin = new TextUtilityPlugin();

        // Act
        var result = plugin.GenerateRandomString();

        // Assert
        result.Should().HaveLength(10);
    }
}
