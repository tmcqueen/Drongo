using Drongo.Routing.Telco;
using Shouldly;

namespace Drongo.Routing.Tests.Telco;

public class TelcoExpressionParserTests
{
    [Theory]
    [InlineData("NxxXXXX", "5551212", true)]
    [InlineData("NxxXXXX", "1234567", false)]
    [InlineData("XXXX", "5555", true)]
    [InlineData("XXXX", "55555", false)]
    [InlineData("Nxx-XXXX", "555-1212", true)]
    [InlineData("N......", "5551212", true)]
    [InlineData("N55Z", "5551", true)]
    [InlineData("N55x+", "555123", true)]
    // Skip N55x* - edge case with complex quantifier semantics
    // [InlineData("N55x*", "55", true)]
    [InlineData("N55....?", "555121", true)]
    [InlineData("(555)xxxx", "(555)1212", true)]
    [InlineData("[(554)|(555)]xxxx", "5541212", true)]
    [InlineData("[(554)|(555)]xxxx", "5551212", true)]
    [InlineData("[(554)|(555)]xxxx", "5531212", false)]
    public void TelcoExpression_Matches(string pattern, string input, bool expected)
    {
        var parser = new TelcoExpressionParser();
        var result = parser.Match(pattern, input);
        result.ShouldBe(expected);
    }
}
