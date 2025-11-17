using System.ComponentModel;
using Microsoft.SemanticKernel;

namespace SlackAiAgent.Plugins;

/// <summary>
/// Plugin that provides mathematical calculation capabilities
/// </summary>
public class CalculatorPlugin
{
    [KernelFunction("add")]
    [Description("Adds two numbers together")]
    public double Add(
        [Description("The first number")] double a,
        [Description("The second number")] double b)
    {
        return a + b;
    }

    [KernelFunction("subtract")]
    [Description("Subtracts the second number from the first")]
    public double Subtract(
        [Description("The first number")] double a,
        [Description("The number to subtract")] double b)
    {
        return a - b;
    }

    [KernelFunction("multiply")]
    [Description("Multiplies two numbers together")]
    public double Multiply(
        [Description("The first number")] double a,
        [Description("The second number")] double b)
    {
        return a * b;
    }

    [KernelFunction("divide")]
    [Description("Divides the first number by the second")]
    public string Divide(
        [Description("The dividend")] double a,
        [Description("The divisor")] double b)
    {
        if (Math.Abs(b) < 0.0000001)
        {
            return "Error: Cannot divide by zero";
        }
        return (a / b).ToString();
    }

    [KernelFunction("power")]
    [Description("Raises a number to a power")]
    public double Power(
        [Description("The base number")] double baseNumber,
        [Description("The exponent")] double exponent)
    {
        return Math.Pow(baseNumber, exponent);
    }

    [KernelFunction("square_root")]
    [Description("Calculates the square root of a number")]
    public string SquareRoot(
        [Description("The number to get the square root of")] double number)
    {
        if (number < 0)
        {
            return "Error: Cannot calculate square root of negative number";
        }
        return Math.Sqrt(number).ToString();
    }

    [KernelFunction("percentage")]
    [Description("Calculates what percentage one number is of another")]
    public string Percentage(
        [Description("The part value")] double part,
        [Description("The whole value")] double whole)
    {
        if (Math.Abs(whole) < 0.0000001)
        {
            return "Error: Cannot calculate percentage with zero as whole";
        }
        var percentage = (part / whole) * 100;
        return $"{percentage:F2}%";
    }
}
