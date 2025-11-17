using System.ComponentModel;
using System.Text;
using Microsoft.SemanticKernel;

namespace SlackAiAgent.Plugins;

/// <summary>
/// Plugin that provides text manipulation and utility functions
/// </summary>
public class TextUtilityPlugin
{
    [KernelFunction("count_words")]
    [Description("Counts the number of words in a text")]
    public int CountWords(
        [Description("The text to count words in")] string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return 0;

        return text.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length;
    }

    [KernelFunction("reverse_text")]
    [Description("Reverses the characters in a text")]
    public string ReverseText(
        [Description("The text to reverse")] string text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        var charArray = text.ToCharArray();
        Array.Reverse(charArray);
        return new string(charArray);
    }

    [KernelFunction("to_uppercase")]
    [Description("Converts text to uppercase")]
    public string ToUppercase(
        [Description("The text to convert")] string text)
    {
        return text.ToUpperInvariant();
    }

    [KernelFunction("to_lowercase")]
    [Description("Converts text to lowercase")]
    public string ToLowercase(
        [Description("The text to convert")] string text)
    {
        return text.ToLowerInvariant();
    }

    [KernelFunction("to_title_case")]
    [Description("Converts text to title case (first letter of each word capitalized)")]
    public string ToTitleCase(
        [Description("The text to convert")] string text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        var textInfo = System.Globalization.CultureInfo.CurrentCulture.TextInfo;
        return textInfo.ToTitleCase(text.ToLower());
    }

    [KernelFunction("encode_base64")]
    [Description("Encodes text to Base64")]
    public string EncodeBase64(
        [Description("The text to encode")] string text)
    {
        var bytes = Encoding.UTF8.GetBytes(text);
        return Convert.ToBase64String(bytes);
    }

    [KernelFunction("decode_base64")]
    [Description("Decodes Base64 text")]
    public string DecodeBase64(
        [Description("The Base64 text to decode")] string base64Text)
    {
        try
        {
            var bytes = Convert.FromBase64String(base64Text);
            return Encoding.UTF8.GetString(bytes);
        }
        catch (FormatException)
        {
            return "Error: Invalid Base64 string";
        }
    }

    [KernelFunction("generate_random_string")]
    [Description("Generates a random string of specified length")]
    public string GenerateRandomString(
        [Description("The length of the random string (default 10)")] int length = 10)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        var random = new Random();
        var result = new StringBuilder(length);

        for (int i = 0; i < length; i++)
        {
            result.Append(chars[random.Next(chars.Length)]);
        }

        return result.ToString();
    }
}
